// ESP8266 or Arduinio reading multiple DHT (11,21,22) Temperature and Humidity Sensors
// (c) D L Bird 2016

#include "DHT.h"   //https://github.com/adafruit/DHT-sensor-library 

DHT dht1(D3, DHT11);
DHT dht3(D4, DHT22);

void setup(void) {
  Serial.begin(9600);
  dht1.begin();
  dht3.begin();
}

void loop() {
  // Read DHT temperature and humidity values
  float DHT11_t = dht1.readTemperature();
  float DHT11_h = dht1.readHumidity();

  float DHT22_t = dht3.readTemperature();
  float DHT22_h = dht3.readHumidity();
    
  Serial.print("DHT11  ");
  Serial.print(DHT11_t,1); Serial.print(String(char(176))+"C  ");
  Serial.print(DHT11_h,1); Serial.println("%RH");
  Serial.println();

  Serial.print("DHT22  ");
  Serial.print(DHT22_t,1); Serial.print(String(char(176))+"C  ");
  Serial.print(DHT22_h,1); Serial.println("%RH");
  Serial.println();

  delay(3000);
}
