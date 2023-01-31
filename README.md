# 2D Lighting Engine

The very first decently working version of a project 8 years in the making... Done within a week from scratch.
More attempts at optimization and more functions (hopefully) to come!

Once you compile and see it in action, you may notice the colors changing strangely as light passes through different "windows". This is a very basic form of simulating light going through colored transparent objects.

The pixel perfection is achieved by taking the liberties given from this approach. Regardless of how much I calculate the different sections of the screen and then paint polygons over them, all the pixels on the screen will be scanned at least once (they have to be drawn often all). Thus, I calculate the color of each pixel as I scan them.

However, that does not mean I am brute-forcing. I have re-written and customized the algorithm for drawing lines and implemented a special algorithm for filling polygons. Also, this approach requires very little mathematics, since the algorithms only care about pixels, and not coordinates - the program has no points to draw, only pixels.

Getting things to run as good as that requires some resourcefulness and trial and error. I have yet to see if I can lower some method calling number, and if I can do any type of branchless programming. If you are asking yourself "Why C#?", I can only answer with "I am mostly accustomed with that language". But I am thinking of trying to move it to C++ before it gets too big and take even more liberties.

I have yet to add:
A light to have a radius
Multiple lights
Light intensity
Color filtering
Flashlight effect
Textured background

I have left unused code to show some of the development stages and some commented code for demonstration.
