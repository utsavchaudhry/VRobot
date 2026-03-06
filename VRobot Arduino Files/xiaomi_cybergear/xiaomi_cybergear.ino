#include "driver/twai.h"
#include "xiaomi_cybergear_driver.h"

// Pins used for the single CAN transceiver
#define RX_PIN D7
#define TX_PIN D6

// Use actual hardware CAN IDs:
static const uint8_t MOTOR_HW_ID_1 = 0x7F;  // hardware ID for motor #1 (e.g., "left" wheel)
static const uint8_t MOTOR_HW_ID_2 = 0x7E;  // hardware ID for motor #2 (e.g., "right" wheel)
static const uint8_t MASTER_CAN_ID = 0x00;  // any valid master ID

// Create two CyberGear driver objects for each hardware ID
XiaomiCyberGearDriver motor1(MOTOR_HW_ID_1, MASTER_CAN_ID);
XiaomiCyberGearDriver motor2(MOTOR_HW_ID_2, MASTER_CAN_ID);

bool driver_installed = false;

#define CMD_DELAY 10  // ms between CAN commands to let motor process each one
#define HEARTBEAT_INTERVAL_MS 500  // resend speed commands to prevent motor watchdog timeout

float current_left_speed = 0.0f;
float current_right_speed = 0.0f;
unsigned long last_command_time = 0;

// Wait for a motor to respond to a status request, retrying up to timeout_ms
bool wait_for_motor(XiaomiCyberGearDriver& motor, unsigned long timeout_ms) {
  unsigned long start = millis();
  while (millis() - start < timeout_ms) {
    motor.request_status();
    // Wait for a CAN response
    twai_message_t rx_msg;
    if (twai_receive(&rx_msg, pdMS_TO_TICKS(200)) == ESP_OK) {
      // Check if this response is from the motor we're waiting for
      uint8_t sender_id = rx_msg.identifier & 0xFF;
      if (sender_id == motor.get_motor_can_id()) {
        return true;
      }
    }
  }
  return false;
}

void setup_motor(XiaomiCyberGearDriver& motor) {
  motor.init_motor(MODE_SPEED);
  delay(CMD_DELAY);
  motor.set_limit_speed(5.0f);
  delay(CMD_DELAY);
  motor.set_limit_current(5.0f);
  delay(CMD_DELAY);
  motor.enable_motor();
  delay(CMD_DELAY);
  motor.set_speed_ref(0.0f);
}

void setup() {
  // Initialize CAN bus (TWAI)
  motor1.init_twai(RX_PIN, TX_PIN, /*serial_debug=*/true);

  // Wait for both motors to be ready before sending config commands.
  // CyberGear motors boot slower than the ESP32.
  Serial.println("Waiting for motor 1...");
  while (!wait_for_motor(motor1, 3000)) {
    Serial.println("Motor 1 not responding, retrying...");
  }
  Serial.println("Motor 1 found!");

  setup_motor(motor1);
  delay(100);

  Serial.println("Waiting for motor 2...");
  while (!wait_for_motor(motor2, 3000)) {
    Serial.println("Motor 2 not responding, retrying...");
  }
  Serial.println("Motor 2 found!");

  setup_motor(motor2);

  driver_installed = true;
  last_command_time = millis();
  Serial.println("Both motors initialized.");
}

void loop() {
  if (!driver_installed) {
    delay(1000);
    return;
  }

  // Check for user input on Serial
  if (Serial.available() > 0) {
    // Read the entire line, e.g. "1.2,2.5"
    String input = Serial.readStringUntil('\n');
    input.trim();

    if (input.equalsIgnoreCase("identify"))
    {
      Serial.println("xiaomi");
      return;
    }

    // Look for the comma that separates [leftSpeed],[rightSpeed]
    int commaPos = input.indexOf(',');
    if (commaPos == -1) {
      Serial.println("Invalid format! Use: [leftSpeed],[rightSpeed]");
      return;
    }

    // Parse the two speeds
    float leftSpeed = input.substring(0, commaPos).toFloat();
    float rightSpeed = input.substring(commaPos + 1).toFloat();

    // Update the motors
    current_left_speed = leftSpeed;
    current_right_speed = rightSpeed;
    motor1.set_speed_ref(-current_left_speed);
    motor2.set_speed_ref(current_right_speed);
    last_command_time = millis();
  }

  // Resend last speed to keep motors alive when no new serial commands arrive
  if (millis() - last_command_time >= HEARTBEAT_INTERVAL_MS) {
    motor1.set_speed_ref(-current_left_speed);
    motor2.set_speed_ref(current_right_speed);
    last_command_time = millis();
  }
}
