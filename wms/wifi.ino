#include <WiFi.h>
#include <Wire.h>
 
const char* ssid = "DESKTOP-BTN25M9 0181";
const char* password =  "8l45!54W";
 
const uint16_t port = 8000;
const char * host = "192.168.137.1";
int count=0;

void setup()
{
  Wire.begin(21, 22);
  Serial.begin(115200);
 
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(100);
    Serial.println("...");
  }
  Serial.print("WiFi connected with IP: ");
  Serial.println(WiFi.localIP());

}
 
void loop()
{
    WiFiClient client;
    
    if (!client.connect(host, port)&&count==0){
        Serial.println("Connection to host failed");
        //delay(100);
        return;
    }
    count=1;
    char rec[24];
    int x=0;
    //Serial.println("Connected to server successful!");
    Wire.requestFrom(8, 12); /* request & read data of size 13 from slave */
    while(Wire.available()){
      rec[x] = Wire.read();
      Serial.print(rec[x],HEX);
      Serial.print(" ");
    /*if(0x19<c>0x7F)Serial.print(c);
    else break;*/
      x++;
      rec[x]=' ';
      x++;
    }
    Serial.println();
 
    client.print(rec);
 
    //Serial.println("Disconnecting...");
    //client.stop();
 
    //delay(1000);
}
/*SSID:  DESKTOP-BTN25M9 0181
通訊協定: Wi-Fi 4 (802.11n)
安全性類型:  WPA2-Personal
網路頻帶: 2.4 GHz
網路通道: 1
連結-本機 IPv6 位址:  fe80::a1ae:2a4b:863f:f665%16
IPv4 位址:  192.168.137.224
IPv4 DNS 伺服器: 192.168.137.1
製造商:  Intel Corporation
描述: Intel(R) Dual Band Wireless-AC 3168
驅動程式版本: 19.51.8.3
實體位址 (MAC): B0-35-9F-62-79-56*/
