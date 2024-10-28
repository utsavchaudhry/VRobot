#include <iostream>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
#include <cmath>
#include <iomanip>

float a1z = 650;
float a2x = 400;
float a2z = 680;
float a3z = 1100;
float a4z = 230;
float a4x = 766;
float a5x = 345;
float a6x = 244;

glm::vec4 tcp_local(0.0f, 0.0f, 0.0f, 1.0f);

int main(int argc, char** argv) {
    
    if (argc != 7) {
        std::cout << "Usage: " << argv[0] << " [J1] [J2] [J3] [J4] [J5] [J6]" << std::endl
            << "J is integer" << std::endl;
    }
    
    float j1 = std::stof(argv[1]) * M_PI / 180.0;
    float j2 = std::stof(argv[2]) * M_PI / 180.0;
    float j3 = std::stof(argv[3]) * M_PI / 180.0;
    float j4 = std::stof(argv[4]) * M_PI / 180.0;
    float j5 = std::stof(argv[5]) * M_PI / 180.0;
    float j6 = std::stof(argv[6]) * M_PI / 180.0;
    
    //base->j1
    glm::mat3 r1 = glm::mat3(
        std::cos(j1), -std::sin(j1), 0.0f,
        std::sin(j1), std::cos(j1), 0.0f,
        0.0f, 0.0f, 1.0f
    ); //z
    glm::mat4 t1 = glm::mat4(r1);
    t1[3][2] = a1z;
    
    //j1->j2
    glm::mat3 r2 = glm::mat3(
        std::cos(j2), 0.0f, std::sin(j2),
        0.0f, 1.0f, 0.0f,
        -std::sin(j2), 0.0f, std::cos(j2)
    ); //y
    glm::mat4 t2 = glm::mat4(r2);
    t2[3][0] = a2x;
    t2[3][2] = a2z;
    
    //j2->j3
    glm::mat3 r3 = glm::mat3(
        std::cos(j3), 0.0f, std::sin(j3),
        0.0f, 1.0f, 0.0f,
        -std::sin(j3), 0.0f, std::cos(j3)
    ); //y
    glm::mat4 t3 = glm::mat4(r3);
    t3[3][2] = a3z;
    
    //j3->j4
    glm::mat3 r4 = glm::mat3(
        1.0f, 0.0f, 0.0f,
        0.0f, std::cos(j4), -std::sin(j4),
        0.0f, std::sin(j4), std::cos(j4)
    ); //x
    glm::mat4 t4 = glm::mat4(r4);
    t4[3][0] = a4x;
    t4[3][2] = a4z;
    
    //j4->j5
    glm::mat3 r5 = glm::mat3(
        std::cos(j5), 0.0f, std::sin(j5),
        0.0f, 1.0f, 0.0f,
        -std::sin(j5), 0.0f, std::cos(j5)
    ); //y
    glm::mat4 t5 = glm::mat4(r5);
    t5[3][0] = a5x;
    
    //j5->j6
    glm::mat3 r6 = glm::mat3(
        1.0f, 0.0f, 0.0f,
        0.0f, std::cos(j6), -std::sin(j6),
        0.0f, std::sin(j6), std::cos(j6)
    ); //x
    glm::mat4 t6 = glm::mat4(r6);
    t6[3][0] = a6x;
    
    glm::mat4 t16 = t1 * t2 * t3 * t4 * t5 * t6;
    
    glm::vec4 tcp_base = t16 * tcp_local;
    
    std::cout << "TCP Position: " << std::round(tcp_base.x) << "," << std::round(tcp_base.y) << "," << std::round(tcp_base.z) << std::endl;
    
    glm::mat3 arm_rot = r1 * r2 * r3;
    glm::mat3 wrist_rot = r4 * r5 * r6;
    glm::mat3 tcp_rot = arm_rot * wrist_rot;
    
    glm::vec3 tcp_euler = glm::vec3(std::atan2(tcp_rot[2][2], tcp_rot[1][2]) * 180.0 / M_PI,
                                    std::atan2(std::sqrt(1 - std::pow(tcp_rot[0][2], 2)), -tcp_rot[0][2]) * 180.0 / M_PI,
                                    std::atan2(tcp_rot[0][0], tcp_rot[0][1]) * 180.0 / M_PI);
    
    std::cout << "TCP Euler: " << std::round(tcp_euler.x) << "," << std::round(tcp_euler.y) << "," << std::round(tcp_euler.z) << std::endl;
    
}
