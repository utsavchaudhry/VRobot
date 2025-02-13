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

void setup() {
  // Initialize CAN bus (TWAI) using the first motor object
  motor1.init_twai(RX_PIN, TX_PIN, /*serial_debug=*/true);
  delay(1000);

  // --- Motor #1 (left) setup ---
  motor1.init_motor(MODE_SPEED);
  motor1.set_limit_speed(5.0f);
  motor1.set_limit_current(5.0f);
  motor1.enable_motor();
  motor1.set_speed_ref(0.0f);

  // --- Motor #2 (right) setup ---
  // If the library fails re-initializing, comment out motor2.init_twai below
  // motor2.init_twai(RX_PIN, TX_PIN, /*serial_debug=*/false);

  motor2.init_motor(MODE_SPEED);
  motor2.set_limit_speed(5.0f);
  motor2.set_limit_current(5.0f);
  motor2.enable_motor();
  motor2.set_speed_ref(0.0f);

  driver_installed = true;
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
    motor1.set_speed_ref(leftSpeed);
    motor2.set_speed_ref(rightSpeed);
  }
}
