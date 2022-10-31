# Procedural Low-Poly Terrain

- This project is using Triangle.Net for Triangulation (it has unfortunately been shut down by now but here is a SnapShot - https://github.com/garykac/triangle.net)



## How does it work?

I am generating a plane and use Poisson Disk Sampling to position the vertices in a non-chaotic but random manner. To add heights onto my terrain i am looping over all the vertices of my plane and then apply Perlin Noise to them. I utilize Triangle.Net to get a triangulated mesh out of these vertices.  To color the mesh I am calulating the average height per Triangle and then evalute it based on a color gradient.


### Want to see a more In-depth-Explanation? Watch this YouTube Tutorial I made!

[![Image alt text here](https://raw.githubusercontent.com/KristinLague/KristinLague.github.io/main/Images/lowPolyTerrainGIF.gif)](https://www.youtube.com/watch?v=sRn8TL3EKDU)


