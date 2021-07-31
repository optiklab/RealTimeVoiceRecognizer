#RealTimeVoiceRecognizer

Speech recognition application which uses private Google API to convert voice speech into text notes.

## What can it be used for?

I created this application in 2011 with using C#, WPF and some of knowledge in C# and Win API interoperability. The main goal was to create application for making fast text notes during Skype calls and meetings. Also I thought it can be helpful for disabled people after improvements in this software.

Application is designed for voice recognition in real time and converting it into a text. It using Google Voice unpublished API. Basically it works as is:
*1. You turn on your microphone
*2. Press Start button
*3. Talk something, for example, have a meeting in Skype
*4. During your talk application switch audio channel on a small pieces and sends it to Google for recognition
*5. Results is visible as text marks at the bottom Text field

The idea was to create something like LOGs for voice meetings, which could be automatically stored in memory more efficiently than audio. Additional bonus is possibility to search on your logs.

Also I was inspired to create this application because I wanted to learn WPF technology and learn how to make low level calls to WinAPI and drawing graphs of the audio channel.

P.S. 2014. For current moment it doesn't not work because Google changed its API to published officially and we need to switch to real API.

![1](https://optiklab.github.io/img/VRec1.png)

![2](https://optiklab.github.io/img/VRec.png)

![3](https://optiklab.github.io/img/VRec2.png)

## LICENSE
This software is published under [GPL v3](http://www.gnu.org/licenses/gpl.txt) license.
For more information please take a look in license [file](https://github.com/optiklab/RealTimeVoiceRecognizer/blob/master/LICENSE.md)

## Building the source

```
git clone git@github.com:optiklab/Spectrometer.git (or https if you use https)
```

### Windows
After cloning the repository, open the solution in VS 2013 and press F5 to compile and run it.

## Run it

To run it, just unpack this package https://googledrive.com/host/0B4Q3U97fHTqIem9DUDJJVWhlbW8/RealTimeVoiceRecognizer.zip

## Questions?
Please contact with me if you have any questions, suggestion and comments.

