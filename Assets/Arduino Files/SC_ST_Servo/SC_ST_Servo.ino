/*
  Merged Arduino Script for Controlling STS and SCS Motors

  This script supports two command sets, determined by a prefix in the serial input:
  
  • Commands beginning with "sts:" use the STS command set:
      - "sts:ping"          : Ping all servos (IDs 1 to 20) and report results.
      - "sts:identify"      : Returns "st" to identify the STS branch.
      - "sts:change [oldID] [newID]"  : Changes a servo’s ID (with EPROM unlocking/locking).
      - "sts:calibrate [id]" : Calibrates the specified servo.
      - "sts:status [id]"    : Retrieves and prints detailed status for the servo.
      - "sts:[id],[position]" : Commands a servo to move to the specified position 
                                using default parameters (speed = 1500, acceleration = 50).
  
  • Commands beginning with "scs:" use the SCS command set:
      - "scs:ping [startID] [endID]" : Pings servo IDs in the specified range.
      - "scs:identify"               : Returns "sc" to identify the SCS branch.
      - "scs:change [oldID] [newID]"   : Changes the servo’s ID (with EPROM unlocking/locking).
      - "scs:status [id]"            : Retrieves and prints status feedback for the servo.
      - "scs:[id],[position]"         : Commands a servo to move to the specified position 
                                      (default acceleration = 0, speed = 1500).
  
  Both branches communicate over Serial1 on GPIO 18 (RX) and GPIO 19 (TX) at 1,000,000 baud.
*/

#include <SCServo.h>

#define S_RXD 18
#define S_TXD 19

// Create an instance for each servo control type.
SMS_STS st;   // STS servo controller instance
SCSCL sc;     // SCS servo controller instance

//---------------------------
// STS Helper Functions
//---------------------------

// Set the position of a servo (STS branch) using default speed and acceleration.
void setServoPosition_STS(int id, int position) {
  int speed = 1500; // default speed
  int acc = 50;     // default acceleration
  st.WritePosEx(id, position, speed, acc);
}

// Change the servo ID (STS branch) by unlocking, writing new ID, and locking.
void changeServoID_STS(int originalID, int newID) {
  st.unLockEprom(originalID);                  // Unlock EPROM-SAFE
  st.writeByte(originalID, SMS_STS_ID, newID);   // Write new ID
  st.LockEprom(newID);                           // Lock EPROM-SAFE
  Serial.print("Changed servo ID from ");
  Serial.print(originalID);
  Serial.print(" to ");
  Serial.println(newID);
}

// Ping all servos (IDs 1 to 20) for STS and report which ones respond.
void pingAllServos_STS() {
  const int maxID = 20;
  int count = 0;
  bool possibleDuplicateID = false;

  Serial.println("Pinging all STS servos...");
  
  for (int id = 1; id <= maxID; id++) {
    int responseID = st.Ping(id);
    if (responseID == id) {
      Serial.print("Servo ID ");
      Serial.print(id);
      Serial.println(" is connected and ready.");
      count++;
    } else if (st.Err != 0) {
      Serial.print("Error pinging servo ID ");
      Serial.print(id);
      Serial.println(". Possible duplicate IDs or communication error.");
      possibleDuplicateID = true;
    }
    st.Err = 0; // Reset error flag for next iteration
  }
  
  if (possibleDuplicateID) {
    Serial.println("Warning: Possible duplicate servo IDs detected.");
  }
  Serial.print("Total servos found: ");
  Serial.println(count);
  if (count == 0) {
    Serial.println("No servos responded to ping.");
  }
}

// Calibrate the specified servo (STS branch) to set its current position as the middle.
void calibrateServo_STS(int id) {
  st.CalibrationOfs(id);
  Serial.print("Calibrated servo ");
  Serial.print(id);
  Serial.println(": current position set as middle.");
}

// Retrieve and display status information from the specified servo (STS branch).
void getServoStatus_STS(int id) {
  int Pos, Speed, Load, Voltage, Temper, Move, Current;

  if (st.FeedBack(id) != -1) {
    Pos     = st.ReadPos(-1);
    Speed   = st.ReadSpeed(-1);
    Load    = st.ReadLoad(-1);
    Voltage = st.ReadVoltage(-1);
    Temper  = st.ReadTemper(-1);
    Move    = st.ReadMove(-1);
    Current = st.ReadCurrent(-1);

    Serial.print("Servo ID: ");
    Serial.println(id);
    Serial.print("Position: ");
    Serial.println(Pos);
    Serial.print("Speed: ");
    Serial.println(Speed);
    Serial.print("Load: ");
    Serial.println(Load);
    Serial.print("Voltage: ");
    Serial.println(Voltage);
    Serial.print("Temperature: ");
    Serial.println(Temper);
    Serial.print("Movement Status: ");
    Serial.println(Move);
    Serial.print("Current: ");
    Serial.println(Current);
    Serial.println();
  } else {
    Serial.print("Failed to get status from servo ID ");
    Serial.println(id);
  }
}

//---------------------------
// SCS Helper Functions
//---------------------------

// Set the position of a servo (SCS branch) using default parameters.
void setServoPosition_SCS(int id, int position) {
  // Default: acceleration = 0, speed = 1500.
  sc.WritePos(id, position, 0, 1500);
}

// Change the servo ID (SCS branch) by unlocking, writing new ID, and locking.
void changeServoID_SCS(int oldID, int newID) {
  sc.unLockEprom(oldID);               // Unlock EPROM-SAFE for the old ID.
  sc.writeByte(oldID, SCSCL_ID, newID);  // Update the servo's ID.
  sc.LockEprom(newID);                 // Lock EPROM-SAFE with the new ID.
}

//---------------------------
// Command Processors
//---------------------------

// Process commands intended for the STS branch.
// The command string should have the "sts:" prefix already removed.
void processSTSCmd(String command) {
  command.trim();
  
  if (command.equalsIgnoreCase("ping")) {
    pingAllServos_STS();
  }
  else if (command.equalsIgnoreCase("identify")) {
    Serial.println("st");
  }
  else if (command.startsWith("change")) {
    // Remove "change" and parse parameters.
    String params = command.substring(6);
    params.trim();
    int spaceIndex = params.indexOf(' ');
    if (spaceIndex != -1) {
      String originalIDStr = params.substring(0, spaceIndex);
      String newIDStr = params.substring(spaceIndex + 1);
      int originalID = originalIDStr.toInt();
      int newID = newIDStr.toInt();
      if (originalID > 0 && newID > 0) {
        changeServoID_STS(originalID, newID);
      } else {
        Serial.println("Invalid IDs provided. Usage: change [original id] [new id]");
      }
    } else {
      Serial.println("Invalid change command. Usage: change [original id] [new id]");
    }
  }
  else if (command.startsWith("calibrate")) {
    // Remove "calibrate" and parse the servo ID.
    String param = command.substring(9);
    param.trim();
    int id = param.toInt();
    if (id > 0) {
      calibrateServo_STS(id);
    } else {
      Serial.println("Invalid calibrate command. Usage: calibrate [id]");
    }
  }
  else if (command.startsWith("status")) {
    // Remove "status" and parse the servo ID.
    String param = command.substring(6);
    param.trim();
    int id = param.toInt();
    if (id > 0) {
      getServoStatus_STS(id);
    } else {
      Serial.println("Invalid status command. Usage: status [id]");
    }
  }
  else {
    // Assume the command is a position command in the form "[id],[position]".
    command.replace(" ", ""); // Remove any spaces.
    int commaIndex = command.indexOf(',');
    if (commaIndex != -1) {
      String idStr = command.substring(0, commaIndex);
      String posStr = command.substring(commaIndex + 1);
      int id = idStr.toInt();
      int position = posStr.toInt();
      if (id > 0) {
        setServoPosition_STS(id, position);
      } else {
        Serial.println("Invalid position command. Expected format: [id],[position]");
      }
    } else {
      Serial.println("Command not recognized in STS mode.");
    }
  }
}

// Process commands intended for the SCS branch.
// The command string should have the "scs:" prefix already removed.
void processSCSCmd(String command) {
  command.trim();
  
  // Command: "ping [start id] [end id]"
  if (command.startsWith("ping")) {
    String params = command.substring(4); // Remove "ping"
    params.trim();
    int firstSpace = params.indexOf(' ');
    if (firstSpace != -1) {
      String startStr = params.substring(0, firstSpace);
      String endStr = params.substring(firstSpace + 1);
      startStr.trim();
      endStr.trim();
      int startID = startStr.toInt();
      int endID = endStr.toInt();
      if (startID > 0 && endID >= startID) {
        Serial.println("Pinging SCS servos...");
        for (int currentID = startID; currentID <= endID; currentID++) {
          int response = sc.Ping(currentID);
          if (response != -1) {
            Serial.print("Servo responded at ID: ");
            Serial.println(response, DEC);
          }
        }
      } else {
        Serial.println("Invalid IDs for ping command. Usage: ping [start id] [end id]");
      }
    } else {
      Serial.println("Usage: ping [start id] [end id]");
    }
  }
  // Command: "[id],[position]"
  else if (command.indexOf(',') != -1) {
    int commaIndex = command.indexOf(',');
    String idStr = command.substring(0, commaIndex);
    String posStr = command.substring(commaIndex + 1);
    idStr.trim();
    posStr.trim();
    int servoID = idStr.toInt();
    int position = posStr.toInt();
    if (servoID > 0) {
      setServoPosition_SCS(servoID, position);
      // Executed silently.
    } else {
      Serial.println("Invalid position command. Expected format: [id],[position]");
    }
  }
  // Command: "change [old id] [new id]"
  else if (command.startsWith("change")) {
    String params = command.substring(6); // Remove "change"
    params.trim();
    int spaceIndex = params.indexOf(' ');
    if (spaceIndex != -1) {
      String oldIdStr = params.substring(0, spaceIndex);
      String newIdStr = params.substring(spaceIndex + 1);
      int oldID = oldIdStr.toInt();
      int newID = newIdStr.toInt();
      if (oldID > 0 && newID > 0) {
        changeServoID_SCS(oldID, newID);
      } else {
        Serial.println("Invalid IDs provided. Usage: change [old id] [new id]");
      }
    } else {
      Serial.println("Invalid change command. Usage: change [old id] [new id]");
    }
  }
  // Command: "status [id]"
  else if (command.startsWith("status")) {
    String param = command.substring(6); // Remove "status"
    param.trim();
    int servoID = param.toInt();
    if (servoID > 0) {
      if (sc.FeedBack(servoID) != -1) {
        int Pos     = sc.ReadPos(-1);
        int Speed   = sc.ReadSpeed(-1);
        int Load    = sc.ReadLoad(-1);
        int Voltage = sc.ReadVoltage(-1);
        int Temper  = sc.ReadTemper(-1);
        int Move    = sc.ReadMove(-1);
        
        Serial.print("Position:");
        Serial.println(Pos);
        Serial.print("Speed:");
        Serial.println(Speed);
        Serial.print("Load:");
        Serial.println(Load);
        Serial.print("Voltage:");
        Serial.println(Voltage);
        Serial.print("Temper:");
        Serial.println(Temper);
        Serial.print("Move:");
        Serial.println(Move);
        delay(10);
      } else {
        Serial.println("FeedBack error");
        delay(500);
      }
    } else {
      Serial.println("Invalid status command. Usage: status [id]");
    }
  }
  else if (command.startsWith("identify")) {
    Serial.println("sc");
  }
  else {
    Serial.println("Command not recognized in SCS mode.");
  }
}

//---------------------------
// Arduino Standard Functions
//---------------------------
void setup() {
  Serial.begin(115200);
  Serial1.begin(1000000, SERIAL_8N1, S_RXD, S_TXD);
  
  // Link the serial port for both controller instances.
  st.pSerial = &Serial1;
  sc.pSerial = &Serial1;
  
  delay(1000);  // Allow time for hardware initialization.
}

void loop() {
  // Process incoming serial data.
  if (Serial.available()) {
    String input = Serial.readStringUntil('\n');
    input.trim();
    
    // Check for the prefix that determines the branch.
    if (input.startsWith("sts:")) {
      // Remove the "sts:" prefix and process as an STS command.
      String cmd = input.substring(4);
      processSTSCmd(cmd);
    }
    else if (input.startsWith("scs:")) {
      // Remove the "scs:" prefix and process as an SCS command.
      String cmd = input.substring(4);
      processSCSCmd(cmd);
    }
    else {
      Serial.println("Error: Command must begin with 'sts:' or 'scs:'");
    }
  }
}
