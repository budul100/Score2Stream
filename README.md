#Score2Stream

Score2Stream is an freeware to read and capture scorebaords of sport events and show its content as tv graphic in video live streams. The idea was inspired by [Scoreboard-webcam-OCR](https://xy-kao.com/projects/scoreboard-ocr-with-python-webcam/) by Xiaoyang Kao.

## Screenshots

![](http://)

## Usage

The configuration of Score2Stream consists of the following steps:

1. Selecting webcam as input source (this can be a video file for testing purposes too).
2. Defining the clips which contain the digits of the scoreboard.
3. Selecting one or multiple clips as a sample template for values.
4. Identifiing samples with values to be used.

### Selecting the input

The input can be selected by clicking *Video -> Source*.

If there is a new webcam added to the computer, then the system must be started again currently to identify the new source.

### Defining a clip

If the source is visible on the video view (left part of the application), then a clip can be selected by clicking *Video -> Add new clip*.

A new clip definition is shown on the editing view (right part of the application). You can define the clip region by selecting the area with the mouse in the video view. To edit the are simply select a new region in the video view.

### Using as template

If a clip is selected, then it can be used as a sample template by clicking *Video -> Use as template*.

The template tab is opened and the clip content is shown on the editing view (right part of the application). Now a new sample value can be added by clicking *Templates -> Add as sample*.