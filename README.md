# HandshakeEmulator
This is a C# interface for an industrial equipment emulator.

# Prerequisites 
OPC server connection and Siemens RTDS libraries. 

# Functionalities

This was created when working off-site and equipment were not available and used for handshake communication testing.

The emulated equipment can:
- reply to an handshake request, in a very trivial manner
- initiate a command, as if the real equipment wanted to perform an action
- set to true some special bits, indicating the status of the equipment (mode, status, errors, permissives...)

The XML prefix and suffix for each equipment are totally configurable. Adding a new equipment , or a new command is a matter of seconds.
On the interface there are some color indications on the current status of the emulated equipment. There is also a log display for additional information on actions or errors.
