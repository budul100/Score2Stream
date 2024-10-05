# Change log

## Version 1.8.2

* ...

## Version 1.8.1

* Tenth of seconds shown optionally only

## Version 1.8.0

* Auto-detection improved
* Verified samples can be filtered
* Number of unverified samples can be restricted now

# 1.7.0

* Multiple clips can be created at once (#20)
* State color set around images (#28)
* Multiple comparison algorithms used by default (#27)
* Area and segment selection improved
* Element order changed to improve element selection
* Sample determination corrected
* Segment type selection corrected
* Web socket delay can be set optionally


# 1.6.2

* Possible exception solved if a sample without clip is selected (#24)
* Opening multiple instances is optional (#23)
* Templates and clips made independent (#21)
* Neighbor detection corrected

## 1.6.1

* Neighbor detection corrected

# 1.6.0

* Splash screen implemented
* Dialog service improved
* Neighbor value detection is optional and can be reset

## Version 1.5.1

* Inputs can be rotated
* Templates can be added directly as menu function
* File handling adjusted to Avalonia 11

## Version 1.5.0

* Cropping considers all elements that do not cover a clipping border
* Template is assigned automatically to a clip if a sample is added (https://github.com/budul100/Score2Stream/issues/16)
* Clips can be selected from each view (https://github.com/budul100/Score2Stream/issues/17)
* Resizing of clips can be undone
* Detection prefers neighbor values

## Version 1.4.0

* Updated to Avalonia 11
* Recognition service added for automatic detection of sample values
* Noise removal added (https://github.com/budul100/Score2Stream/issues/9)
* Zoom and pan of video view implemented
* Templates are independent of samples
* Samples can be ordered
* Request is shown if a sample with value shall be removed
* Number of periods is saved in settings
* Menu entries reordered
* Image correction steps reordered
* Ticker handling corrected

## Version 1.3.0

* Templates and samples saved in settings
* Detection mode is automatically disabled when then samples tab is deactivated
* Background color of detection mode changed
* Clip selected if clip property is changed
* Clip types updated if clip is removed
* Scoreboard contents shown in title bar
* Similarity shown in all views