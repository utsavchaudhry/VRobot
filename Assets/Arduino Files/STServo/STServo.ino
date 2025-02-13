/*
This script combines servo control, ID changing, ping, calibration, and status functionality.
It reads serial input to set servo positions, change servo IDs, ping servos, calibrate servos, and get servo status.
*/

#include <SCServo.h>

SMS_STS st;

// the UART used to control servos.
// GPIO 18 - S_RXD, GPIO 19 - S_TXD, as default.
#define S_RXD 18
#define S_TXD 19

void setup()
{
  Serial.begin(115200);
  Serial1.begin(1000000, SERIAL_8N1, S_RXD, S_TXD);
  st.pSerial = &Serial1;
  delay(1000);
}

// Helper function to set the position of a servo with given ID
void setServoPosition(int id, int position)
{
  int speed = 1500; // default speed
  int acc = 50;     // default acceleration
  st.WritePosEx(id, position, speed, acc);
}

// Helper function to change the ID of a servo
void changeServoID(int originalID, int newID)
{
  st.unLockEprom(originalID);                  // Unlock EPROM-SAFE
  st.writeByte(originalID, SMS_STS_ID, newID); // Change ID
  st.LockEprom(newID);                         // Lock EPROM-SAFE
  Serial.print("Changed servo ID from ");
  Serial.print(originalID);
  Serial.print(" to ");
  Serial.println(newID);
}

// Helper function to ping all servos
void pingAllServos()
{
  const int maxID = 20; // Maximum servo ID
  int respondingIDs[20]; // Array to store responding IDs
  int count = 0;
  bool possibleDuplicateID = false;

  Serial.println("Pinging all servos...");
  
  for (int id = 1; id <= maxID; id++)
  {
    int responseID = st.Ping(id);
    if (responseID == id)
    {
      respondingIDs[count++] = id;
      Serial.print("Servo ID ");
      Serial.print(id);
      Serial.println(" is connected and ready.");
    }
    else if (st.Err != 0)
    {
      Serial.print("Error pinging servo ID ");
      Serial.print(id);
      Serial.println(". Possible duplicate IDs or communication error.");
      possibleDuplicateID = true;
    }
    // Reset error flag for next iteration
    st.Err = 0;
  }

  if (possibleDuplicateID)
  {
    Serial.println("Warning: Possible duplicate servo IDs detected.");
  }

  Serial.print("Total servos found: ");
  Serial.println(count);

  if (count == 0)
  {
    Serial.println("No servos responded to ping.");
  }
}

// Helper function to calibrate a servo
void calibrateServo(int id)
{
  st.CalibrationOfs(id);
  Serial.print("Calibrated servo ");
  Serial.print(id);
  Serial.println(": current position set as middle.");
}

// Helper function to get status of a servo
void getServoStatus(int id)
{
  int Pos;
  int Speed;
  int Load;
  int Voltage;
  int Temper;
  int Move;
  int Current;

  if(st.FeedBack(id) != -1)
  {
    Pos = st.ReadPos(-1);
    Speed = st.ReadSpeed(-1);
    Load = st.ReadLoad(-1);
    Voltage = st.ReadVoltage(-1);
    Temper = st.ReadTemper(-1);
    Move = st.ReadMove(-1);
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
  }
  else
  {
    Serial.print("Failed to get status from servo ID ");
    Serial.println(id);
  }
}

void loop()
{
  if (Serial.available())
  {
    String input = Serial.readStringUntil('\n');
    input.trim(); // Remove any leading/trailing whitespace

    if (input.equalsIgnoreCase("ping"))
    {
      // Ping all servos
      pingAllServos();
    }
    else if (input.equalsIgnoreCase("identify"))
    {
      Serial.println("st");
    }
    else if (input.startsWith("change"))
    {
      // Parse change command
      input = input.substring(6); // Remove "change"
      input.trim();
      int spaceIndex = input.indexOf(' ');
      if (spaceIndex != -1)
      {
        String originalIDStr = input.substring(0, spaceIndex);
        String newIDStr = input.substring(spaceIndex + 1);
        int originalID = originalIDStr.toInt();
        int newID = newIDStr.toInt();
        changeServoID(originalID, newID);
      }
      else
      {
        Serial.println("Invalid change command. Usage: change [original id] [new id]");
      }
    }
    else if (input.startsWith("calibrate"))
    {
      // Parse calibrate command
      input = input.substring(9); // Remove "calibrate"
      input.trim();
      int id = input.toInt();
      if (id > 0)
      {
        calibrateServo(id);
      }
      else
      {
        Serial.println("Invalid calibrate command. Usage: calibrate [id]");
      }
    }
    else if (input.startsWith("status"))
    {
      // Parse status command
      input = input.substring(6); // Remove "status"
      input.trim();
      int id = input.toInt();
      if (id > 0)
      {
        getServoStatus(id);
      }
      else
      {
        Serial.println("Invalid status command. Usage: status [id]");
      }
    }
    else
    {
      input.replace(" ", ""); // Remove any spaces
      char inputArray[input.length() + 1];
      input.toCharArray(inputArray, sizeof(inputArray));
      char *token = strtok(inputArray, ",");
      if (token != NULL)
      {
        int id = atoi(token);
        token = strtok(NULL, ",");
        if (token != NULL)
        {
          int position = atoi(token);
          setServoPosition(id, position);
        }
      }
    }
  }
}
