CircleCounter
=============

A C# library with a companion console application that detects circles in an image.
To detect the circles, I have used my own algorithm that I call 'Wiggle Method'. It works by checking a small imaginary circle against the image. If that small circle is detected to be inside a large one, it grows and wiggles until there is no more room left to wiggle. Once that happens, the once small circle should have completely taken the shape and size of its containing circle.
