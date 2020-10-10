#include <SoftwareSerial.h> //Used for transmitting to the device
#include <Wire.h>
SoftwareSerial softSerial(2, 3); //RX, TX
#include "SparkFun_UHF_RFID_Reader.h" //Library for controlling the M6E Nano module
RFID nano; //Create instance

bool conn;
int myint[12];
char mychar[12];

void setup()
{
  Serial.begin(115200);
  Wire.begin(8);                /* join i2c bus with address 8 */
  //Wire.onReceive(receiveEvent); /* register receive event */
  Wire.onRequest(requestEvent); /* register request event */
            /* start serial for debug */
  while (!Serial);
  Serial.println();
  Serial.println("Initializing...");

  if (setupNano(38400) == false) //Configure nano to run at 38400bps
  {
    Serial.println(F("Module failed to respond. Please check wiring."));
    conn=false;
    return; //Freeze!
  }
  else conn=true;

  nano.setRegion(REGION_NORTHAMERICA); //Set to North America

  nano.setReadPower(2200); //5.00 dBm. Higher values may cause USB port to brown out
  //Max Read TX Power is 27.00 dBm and may cause temperature-limit throttling
}

void loop()
{
  //Serial.println(F("Press a key to scan for a tag"));
  //while (!Serial.available()); //Wait for user to send a character
  //Serial.read(); //Throw away the user's character

  byte responseType = 0;
  byte myEPC[12]; //Most EPCs are 12 bytes
  byte myEPClength;
  
  while (responseType != RESPONSE_SUCCESS)//RESPONSE_IS_TAGFOUND)
  {
    myEPClength = sizeof(myEPC); //Length of EPC is modified each time .readTagEPC is called

    responseType = nano.readTagEPC(myEPC, myEPClength, 500); //Scan for a new tag up to 500ms
  }  
  //Serial.print( myEPClength);
  //Print EPC
  ///Serial.print(F(" epc["));
  for (int x = 0 ; x < myEPClength ; x++)
  {
    myint[x]=myEPC[x];
    //if(myint[x]>127)myint[x]=myint[x]-128;
    mychar[x]=char(myint[x]-128);
    //if (myEPC[x] < 0x10) Serial.print(F("0"));
    ///Serial.print(mychar[x],HEX);
    ///Serial.print(F(" "));
  }
  ///Serial.println(F("]"));
  
}

//Gracefully handles a reader that is already configured and already reading continuously
//Because Stream does not have a .begin() we have to do this outside the library
boolean setupNano(long baudRate)
{
  nano.begin(softSerial); //Tell the library to communicate over software serial port

  //Test to see if we are already connected to a module
  //This would be the case if the Arduino has been reprogrammed and the module has stayed powered
  softSerial.begin(baudRate); //For this test, assume module is already at our desired baud rate
  while(!softSerial); //Wait for port to open

  //About 200ms from power on the module will send its firmware version at 115200. We need to ignore this.
  while(softSerial.available()) softSerial.read();
  
  nano.getVersion();

  if (nano.msg[0] == ERROR_WRONG_OPCODE_RESPONSE)
  {
    //This happens if the baud rate is correct but the module is doing a ccontinuous read
    nano.stopReading();

    Serial.println(F("Module continuously reading. Asking it to stop..."));

    delay(1500);
  }
  else
  {
    //The module did not respond so assume it's just been powered on and communicating at 115200bps
    softSerial.begin(115200); //Start software serial at 115200

    nano.setBaud(baudRate); //Tell the module to go to the chosen baud rate. Ignore the response msg

    softSerial.begin(baudRate); //Start the software serial port, this time at user's chosen baud rate
  }

  //Test the connection
  nano.getVersion();
  if (nano.msg[0] != ALL_GOOD) return (false); //Something is not right

  //The M6E has these settings no matter what
  nano.setTagProtocol(); //Set protocol to GEN2

  nano.setAntennaPort(); //Set TX/RX antenna ports to 1

  return (true); //We are ready to rock
}

// function that executes whenever data is requested from master
void requestEvent() {
  
  //Wire.write(01);
  ///Wire.write(mychar);
  
  if(conn==false){
    Serial.println("module fail");
    Wire.write("module fail"); 
    //while(1);
  }
  else
  {
    Wire.write(mychar);
    for(int i=0; i<sizeof(mychar); i++)
    {
      Serial.print(mychar[i],HEX);
    }
    Serial.println("!");
  }
  for (byte x = 0 ; x < 12 ; x++)
  {
    mychar[x]=0;
  }
  /*send string on request */
}
