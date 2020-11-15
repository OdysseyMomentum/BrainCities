#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <Adafruit_Sensor.h>
#include "DHT.h"          

#define wifi_ssid "<WIFI SSID>"
#define wifi_password "<WIFI SECRET>"

#define mqtt_server "<BROKER IP>"
#define mqtt_user "<BROKER USER>"  
#define mqtt_password "<BROKER PASSWORD>" 

#define temperature_topic "sensor/0/temperature"  
#define humidity_topic "sensor/0/humidity"     
#define sub_topic "dashboard/sensor/0/action"     

// to decode messages
char message_buff[100];

long lastMsg = 0;   //timestamp of last published message
long lastRecieved = 0;
bool debug = true;  

#define LED D2 
#define DHTPIN D4   

// Choose one below depending on your component
//#define DHTTYPE DHT11       // DHT 11 
#define DHTTYPE DHT22         // DHT 22  (AM2302)


DHT dht(DHTPIN, DHTTYPE);     
WiFiClient espClient;
PubSubClient client(espClient);

void setup() {
  Serial.begin(9600);     // optional
  pinMode(LED,OUTPUT);  
  setup_wifi();           // connect to wifi
  client.setServer(mqtt_server, 1883);    //configure mqtt connection
  client.setCallback(callback);  //callback method used when recieving messages
  dht.begin();
}

//Wifi connection
void setup_wifi() {
  delay(10);
  Serial.println();
  Serial.print("Connecting to ");
  Serial.println(wifi_ssid);

  WiFi.begin(wifi_ssid, wifi_password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("");
  Serial.print("IP Address : ");
  Serial.print(WiFi.localIP());
}

// reconnect
void reconnect() {
  // loop until connection is done
  while (!client.connected()) {
    digitalWrite(LED, HIGH);
    delay(500);
    Serial.print("Connecting MQTT server...");
    if (client.connect("sensor_0", mqtt_user, mqtt_password)) {
      Serial.println("OK");
      digitalWrite(LED, HIGH);
    } else {
      Serial.print("Error : ");
      Serial.print(client.state());
      Serial.println("Waiting 5 seconds before trying to reconnect.");
      digitalWrite(LED, LOW);
      delay(4500);
    }
  }
}

void loop() {
  if (!client.connected()) {
    
    reconnect();
  }else{
    client.loop();
  }

  long now = millis();
  //send a message every minute
  if (now - lastMsg > 1000 * 60) {
    lastMsg = now;
    //Read huminity
    float h = dht.readHumidity();
    // read temperature Celcius
    float t = dht.readTemperature();

    // stop if values are not correct
    if ( isnan(t) || isnan(h)) {
      Serial.println("Error reading, please check DHT sensor");
      return;
    }
  
    if ( debug ) {
      Serial.print("Temperature : ");
      Serial.print(t);
      Serial.print(" | Humidity : ");
      Serial.println(h);
    }  
    if (client.connected()){
      client.publish(temperature_topic, String(t).c_str(), true);   //Publish temperature on topic
      client.publish(humidity_topic, String(h).c_str(), true);      //Publish humidity on topic
    }
  }
  if (now - lastRecieved > 100 ) {
    lastRecieved = now;
    if (client.connected()){
      client.subscribe(sub_topic);
    }
  }
}

// Triggered action when message is received
void callback(char* topic, byte* payload, unsigned int length) {

  int i = 0;
  if ( debug ) {
    Serial.println("New message from topic: " + String(topic));
    Serial.print(" | length: " + String(length,DEC));
  }
  // create character buffer with ending null terminator (string)
  for(i=0; i<length; i++) {
    message_buff[i] = payload[i];
  }
  message_buff[i] = '\0';
  
  String msgString = String(message_buff);
  if ( debug ) {
    Serial.println("Payload: " + msgString);
  }
  
  if ( msgString == "ON" ) {
    digitalWrite(D2,HIGH);  
  } else {
    digitalWrite(D2,LOW);  
  }
}
