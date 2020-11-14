## configure MQTT Brocker

***Documentation from [IoTStack](https://sensorsiot.github.io/IOTstack/Containers/Mosquitto/)***

Open a shell in the mosquitto container:

``
$ docker exec -it mosquitto sh
``

In the following, replace **«MYUSER»** with the username you want to use for controlling access to Mosquitto and then run these commands:

``
$ mosquitto_passwd -c /mosquitto/pwfile/pwfile «MYUSER» $ exit
``

mosquitto_passwd will ask you to type a password and confirm it.

The path on the right hand side of: ***-c /mosquitto/pwfile/pwfile*** is inside the container. Outside the container, it maps to:

``
./volumes/mosquitto/pwfile/pwfile
``

You should be able to see the result of setting a username and password like this:

``
$ cat ./volumes/mosquitto/pwfile/pwfile MYUSER:$6$lBYlxjWtLON0fm96$3qgcEyr/nKvxk3C2Jk36kkILJK7nLdIeLhuywVOVkVbJUjBeqUmCLOA/T6qAq2+hyyJdZ52ALTi+onMEEaM0qQ== $
``

Open **mosquitto.conf** in a text editor. Find this line:

``
password_file /mosquitto/pwfile/pwfile
``

Remove the # in front of password_file. Save.

Restart Mosquitto:

``
$ docker-compose restart mosquitto
``

Use the new credentials where necessary.

**Notes:**

You can revert to password-disabled state by going back to step 3, re-inserting the "#", then restarting Mosquitto as per step 4.
If mosquitto keeps restarting after you implement password checking, the most likely explanation will be something wrong with the password file. Implement the advice in the previous note.