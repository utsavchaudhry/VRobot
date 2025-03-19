/*
  This program supports four commands sent over the serial monitor:

  1. "ping [start id] [end id]"
     - Pings servo IDs from [start id] to [end id] and prints only those that respond.
       (Example: "ping 10 30" pings IDs 10 through 30.)

  2. "[id],[position]"
     - Commands a servo to move to the specified position (e.g., "1,200" moves servo with ID 1 to position 200)
       using default parameters: acceleration = 0, speed = 1500.
       Executes silently.

  3. "change [old id] [new id]"
     - Changes the servo's ID from old id to new id by unlocking the EPROM, writing the new ID,
       and re-locking the EPROM.
       Executes silently.

  4. "status [id]"
     - Retrieves and prints servo feedback for the specified servo ID, including position, speed,
       load, voltage, temperature, and move status.
       
  The UART used for controlling servos is configured on:
  GPIO 18 - S_RXD, GPIO 19 - S_TXD.
*/

#define S_RXD 18
#define S_TXD 19

#include <SCServo.h>

// Create an instance of the SCServo library.
SCSCL sc;

void setup()
{
  // Initialize the primary serial port for computer communication.
  Serial.begin(115200);

  // Initialize Serial1 for servo communication with the specified baud rate and pins.
  Serial1.begin(1000000, SERIAL_8N1, S_RXD, S_TXD);
  sc.pSerial = &Serial1;
  
  // Allow time for hardware initialization.
  delay(1000);
}

void loop()
{
  // Process incoming commands when available.
  if (Serial.available() > 0)
  {
    // Read the incoming command until a newline is encountered.
    String command = Serial.readStringUntil('\n');
    command.trim();  // Remove extraneous whitespace.

    // 1. "ping [start id] [end id]" command:
    // Check if command starts with "ping". Note that this now expects two additional parameters.
    if (command.startsWith("ping"))
    {
      // Remove the "ping" part from the command.
      command.remove(0, 4);
      command.trim();
      
      // Expect two tokens: start and end IDs.
      int firstSpace = command.indexOf(' ');
      if (firstSpace != -1)
      {
        String startStr = command.substring(0, firstSpace);
        String endStr = command.substring(firstSpace + 1);
        startStr.trim();
        endStr.trim();
        
        int startID = startStr.toInt();
        int endID = endStr.toInt();
        
        // Ping each servo ID from startID to endID.
        for (int currentID = startID; currentID <= endID; currentID++)
        {
          int response = sc.Ping(currentID);
          if (response != -1)
          {
            Serial.print("Servo responded at ID: ");
            Serial.println(response, DEC);
          }
        }
      }
      else
      {
        // If parameters are not provided, instruct the user on the proper usage.
        Serial.println("Usage: ping [start id] [end id]");
      }
    }
    // 2. "[id],[position]" command:
    else if (command.indexOf(',') != -1)
    {
      int commaIndex = command.indexOf(',');
      String idStr = command.substring(0, commaIndex);
      String posStr = command.substring(commaIndex + 1);
      idStr.trim();
      posStr.trim();

      int servoID = idStr.toInt();
      int position = posStr.toInt();
      
      // Command the servo to move to the specified position with default parameters:
      // acceleration = 0, max speed = 1500.
      sc.WritePos(servoID, position, 0, 1500);
      // Executed silently.
    }
    // 3. "change [old id] [new id]" command:
    else if (command.startsWith("change"))
    {
      // Remove the "change" keyword.
      command.remove(0, 6);
      command.trim();
      
      int firstSpace = command.indexOf(' ');
      if (firstSpace != -1)
      {
        String oldIdStr = command.substring(0, firstSpace);
        String newIdStr = command.substring(firstSpace + 1);
        oldIdStr.trim();
        newIdStr.trim();
        
        int oldId = oldIdStr.toInt();
        int newId = newIdStr.toInt();
        
        // Execute the ID change procedure:
        // Unlock EPROM, update the ID, and re-lock EPROM.
        sc.unLockEprom(oldId);               // Unlock EPROM-SAFE for the old ID.
        sc.writeByte(oldId, SCSCL_ID, newId);  // Update the servo's ID.
        sc.LockEprom(newId);                 // Lock EPROM-SAFE with the new ID.
      }
    }
    // 4. "status [id]" command:
    else if (command.startsWith("status"))
    {
      // Remove the "status" keyword.
      command.remove(0, 6);
      command.trim();
      
      int servoID = command.toInt();
      
      // Retrieve and display the feedback for the specified servo.
      if (sc.FeedBack(servoID) != -1)
      {
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
      }
      else
      {
        Serial.println("FeedBack err");
        delay(500);
      }
    }
    else if (command.startsWith("identify")) 
    {
      Serial.println("sc");
    }
    // Unrecognized command:
    else
    {
      Serial.println("Command not recognized.");
    }
  }
}
