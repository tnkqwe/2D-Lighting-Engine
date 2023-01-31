using System.Diagnostics;
using System.Windows.Forms;

namespace _2D_Lighting_Engine
{
    public partial class Form1 : Form
    {
        private class LightBlock//an object that absorbs light
        {
            public LightBlock(int sX, int sY, int eX, int eY, Color color)
            {
                start = new Point(sX, sY);
                end = new Point(eX, eY);

                this.color = color;

                methods.setEquasion(ref A, ref B, ref C, start, end);
            }

            public Point start;
            public Point end;
            public Color color;

            public int A = 0, B = 0, C = 0;// A * x + B * y + C = 0; the expression of the straight line, on which the ends lie on; ihere I use it to determine if two lines intersect, without finding the intersection point
        }

        private Point lightsource;
        private LightBlock[] lightBlocks;
        //private Bitmap whiteImage;

        private int width, height;//total screen size
        int size = 10;//multiplier

        Stopwatch timer;//used to check render times
        public Form1()
        {
            InitializeComponent();

            lightBlocks = new LightBlock[8];
            lightBlocks[0] = new LightBlock(40 * size, 34 * size, 2 * size, 32 * size, Color.Red);
            lightBlocks[1] = new LightBlock(30 * size, 4 * size, 20 * size, 20 * size, Color.Blue);
            lightBlocks[2] = new LightBlock(80 * size, 75 * size, 25 * size, 70 * size, Color.FromArgb(0, 255, 0));
            lightBlocks[3] = new LightBlock(30 * size, 50 * size, 20 * size, 40 * size, Color.Cyan);
            lightBlocks[4] = new LightBlock(30 * size, 80 * size, 4 * size, 80 * size, Color.Yellow);
            lightBlocks[5] = new LightBlock(63 * size, 30 * size, 62 * size, 60 * size, Color.Magenta);
            lightBlocks[6] = new LightBlock(40 * size, 50 * size, 60 * size, 50 * size, Color.Gray);
            lightBlocks[7] = new LightBlock(50 * size, 30 * size, 50 * size, 60 * size, Color.LightGray);

            lightsource = new Point(50 * size, 50 * size);

            width = 160 * size;
            height = 90 * size;

            pictureBox1.Image = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            isDrawn = new bool[0];
            imageData = new byte[0];

            timer1.Interval = 1;//timer letting the program to run as hard as possible; there should be a better way
            timer1.Enabled = true;

            timer = new Stopwatch();
        }

        private bool[] isDrawn;//is the pixel drawn
        private byte[] imageData;//color of the pixels

        LinkedList<int> pointsOfInterest;//points to start scanning pixels from
        private void redraw()//cycle
        {
            isDrawn = new bool[width * height];//the sizes of multidimentional arrays have an impact on the performance; I do not know for sure if it applies for a bool array

            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);
            //https://learn.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.lockbits?view=windowsdesktop-7.0

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            imageData = new byte[bytes];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, imageData, 0, bytes);

            imageData = new byte[bytes];

            pointsOfInterest = new LinkedList<int>();//you can also try a List, but expect it to be slower
            pointsOfInterest.AddLast(lightsource.Y * width + lightsource.X);
            //fasten your seatbelts!
            for (int i = 0; i < lightBlocks.Length; i++)
            {
                drawLine(lightBlocks[i].start, lightBlocks[i].end, lightBlocks[i].color.R, lightBlocks[i].color.G, lightBlocks[i].color.B);
            }

            for (int i = 0; i < lightBlocks.Length; i++)
            {
                drawRay(lightsource, lightBlocks[i].start, 255, 255, 255);
                drawRay(lightsource, lightBlocks[i].end, 255, 255, 255);
            }

            byte R = 255, G = 255, B = 255;

            //de-comment this section to see the difference between plotting pixels to start painting from and scanning all pixels
            //for (int w = 0; w < width; w++)
            //{
            //    for (int h = 0; h < height; h++)
            //    {
            //        //int pixel = h * width + w;
            //        if (!isDrawn[h * width + w])
            //        {
            //            pixelColor(w, h, ref R, ref G, ref B);//find what light reaches the pixel
            //            fillArea(w, h, R, G, B);//fill the polygon

            //            //you can have a little slideshow if you de-comment the following rows and the row where 'pixel'gets calculated, and comment the previous row
            //            /*isDrawn[pixel] = true;
            //            imageData[pixel * 3] = B;
            //            imageData[pixel * 3 + 1] = G;
            //            imageData[pixel * 3 + 2] = R;*/
            //            //it is the differece between filling a polygon and having to scan each and every pixel for its color 
            //        }
            //    }
            //}
            int x = -1, y = -1;
            foreach (int i in pointsOfInterest)//all pixels are not needed to be scanned
            {//I have yet to see if I can use a faster way to loop through or use a better structure
                if (!isDrawn[i])
                {
                    x = i % width;
                    y = i / width;

                    pixelColor(x, y, ref R, ref G, ref B);
                    fillArea(x, y, R, G, B);
                }
            }

            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(imageData, 0, ptr, bytes);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            pictureBox1.Image = bmp;
        }

        private void drawLine(Point start, Point end, byte r, byte g, byte b)
        {
            if (start.X >= width || start.X < 0 ||
                start.Y >= height || start.Y < 0 ||
                end.X >= width || end.X < 0 ||
                end.Y >= height || end.Y < 0)
                throw new ArgumentException("One of the line's ends goes outside of the picture!");//this can be ignored if I put better conditions

            drawLine(start, end, r, g, b, false);
        }
        private void drawRay(Point source, Point target, byte r, byte g, byte b)
        {
            drawLine(source, target, r, g, b, true);
        }

        private void drawLine(Point source, Point target, byte r, byte g, byte b, bool isRay)
        {//will draw from point 0;0 and then move points to their respected locations
            //if (color.Equals(Color.Green)) { int k = 0; k = 0; }//here be breakpoint

            Point vector = new Point(target.X - source.X, target.Y - source.Y);//the differences between the points; used to find the direction of the line
            int spin = 0;//I am not good with names
            //spin == 0 - line going right; x increases faster than |y|
            //spin == 1 - line going up;   |y| increases faster than x; y is negative
            //spin == 2 - line going left; |x| increases faster than |y|; both x and y are negative
            //spin == 3 - line going down;  y increases faster than |x|; x is negative
            int tempX = 0, tempY = 0;//the end of a temporary line
            int maxX = 0, maxY = 0;//the maximum coordinate values
            setTempPointSpin(ref tempX, ref tempY, ref maxX, ref maxY, ref spin, vector, source, isRay);
            //here a temporary line gets calculated; it starts from 0,0, gets drawn to the right and is as long as the passed line

            int direction = 0;//line moves down and right
            if (vector.X > 0 && vector.Y < 0)
                direction = 1;//up and right
            else if (vector.X < 0 && vector.Y < 0)
                direction = 2;//left and up
            else if (vector.X < 0 && vector.Y > 0)
                direction = 3;//down and left

            //this is a customized copy of the pseudo code in Wikipedia
            int dx = tempX;
            int dy = tempY;

            int yi = 1;//slope
            if (dy < 1) { yi = -1; dy = -dy; }

            int D = 2 * dy - dx;
            int y = 0;

            int x = 0;
            if (isRay)//if the line is a ray, it will be painted until it hits a screen edge
            {//and also it can get drawn from the end of a light block
                x = tempX;
                y = tempY;

                pixelColor(target.X, target.Y, ref r, ref g, ref b);
            }
            for (; x <= maxX && Math.Abs(y) <= Math.Abs(maxY) && Math.Abs(y) < height; x++)//the loop can break when it reaches the end of the line, or when it reaches the ends of the screen
            {
                //the other opton is to write different versions for all four directions
                //it would probably be faster and less messy, but at the time of writing, I was not concerned with that
                //either way, the most calculations happen during plotting the points,
                //while the pre-calculations are done only once per line
                //pixel plotting==========================================================================
                int resultX, resultY;//put pixels back in their place
                if (spin == 0)
                {
                    resultX = x + source.X;
                    resultY = y + source.Y;
                }
                else if (spin == 1)
                {
                    resultX = y + source.X;
                    resultY = -x + source.Y;
                }
                else if (spin == 2)
                {
                    resultX = -x + source.X;
                    resultY = -y + source.Y;
                }
                else if (spin == 3)
                {
                    resultX = -y + source.X;
                    resultY = x + source.Y;
                }
                else
                    throw new Exception("Something is wrong here...");

                int pixel = resultY * width + resultX;

                if (isDrawn[pixel])
                {
                    if (isRay)
                        filterColor(pixel, ref r, ref g, ref b);//the ray has passed a painted pixel, meaning that it has crossed a light block

                    int upperRow = pixel - width;
                    int lowerRow = pixel + width;
                }
                else
                {
                    isDrawn[pixel] = true;
                    imageData[pixel * 3] = b;//pixel color is stored in the reverse order
                    imageData[pixel * 3 + 1] = g;//do not ask
                    imageData[pixel * 3 + 2] = r;//found it by trial and error

                    if (isRay)
                        filterRay(resultX, resultY, ref r, ref g, ref b, direction);//two lines can intersect without sharing a pixel
                }
                //written like this to lower the number of method calls==============================================
                if (D > 0)
                {
                    y = y + yi;
                    D = D + (2 * (dy - dx));
                    //every time the line is about to move a row, plot two points of interest - one on the next pixel on the same row and one on the next row at the same column
                    if (vector.X >= 0)
                    {
                        if (resultX < width - 1)
                            pointsOfInterest.AddLast(resultY * width + resultX + 1);
                        if (vector.Y >= 0)
                        {
                            if (resultY < height - 1)
                                pointsOfInterest.AddLast((resultY + 1) * width + resultX);
                        }
                        else
                        {
                            if (resultY > 0)
                                pointsOfInterest.AddLast((resultY - 1) * width + resultX);
                        }
                    }
                    else
                    {
                        if (resultX > 0)
                            pointsOfInterest.AddLast(resultY * width + resultX - 1);
                        if (vector.Y >= 0)
                        {
                            if (resultY < height - 1)
                                pointsOfInterest.AddLast((resultY + 1) * width + resultX);
                        }
                        else
                        {
                            if (resultY > 0)
                                pointsOfInterest.AddLast((resultY - 1) * width + resultX);
                        }
                    }
                }
                else
                    D = D + 2 * dy;
            }
        }
        private void setTempPointSpin(
            ref int tempX, ref int tempY,
            ref int maxX, ref int maxY,
            ref int spin, Point vector, Point start, bool isRay)
        {
            int tempMaxX = vector.X;//the end of the temporary line
            int tempMaxY = vector.Y;

            if (isRay)//if it is ray, the end is at a screen edge
            {
                if (vector.X > 0) tempMaxX = width - start.X - 1;
                else tempMaxX = -start.X;
                if (vector.Y > 0) tempMaxY = height - start.Y - 1;
                else tempMaxY = -start.Y;
            }
            //the temporary line must have its start at 0,0 and end have a positive X coordinate
            if (vector.X >= 0 && vector.X >= Math.Abs(vector.Y))
            {
                spin = 0;
                tempX = vector.X;
                tempY = vector.Y;

                maxX = tempMaxX;
                maxY = tempMaxY;
            }
            else if (vector.Y < 0 && Math.Abs(vector.Y) >= Math.Abs(vector.X))
            {
                spin = 1;
                tempX = -vector.Y;
                tempY = vector.X;

                maxX = -tempMaxY;
                maxY = tempMaxX;
            }
            else if (vector.X < 0 && Math.Abs(vector.X) >= Math.Abs(vector.Y))
            {
                spin = 2;
                tempX = -vector.X;
                tempY = -vector.Y;

                maxX = -tempMaxX;
                maxY = -tempMaxY;
            }
            else if (vector.Y >= 0 && vector.Y >= Math.Abs(vector.X))
            {
                spin = 3;
                tempX = vector.Y;
                tempY = -vector.X;

                maxX = tempMaxY;
                maxY = -tempMaxX;
            }
            else
                throw new Exception("Something is wrong here...");//no problems so far
        }
        private void filterRay(int x, int y, ref byte R, ref byte G, ref byte B, int direction)//check if the ray is about to cross a light block, without sharing a pixel
        {
            //if the line is moving on the edge of the screen, then it will either share a pixel with any line it crosses or just end
            if (direction == 0 && (x == width - 1 || y == height - 1))
                return;
            if (direction == 1 && (x == width - 1 || y == 0))
                return;
            if (direction == 2 && (x == 0 || y == 0))
                return;
            if (direction == 3 && (x == 0 || y == height - 1))
                return;
            //check two pixels in the respected direction
            //it can be done without rotating the pixels back
            int pixelInd = y * width + x;

            if (direction == 0 && isDrawn[y * width + x + 1] && isDrawn[(y + 1) * width + x])
            {
                filterColor(pixelInd + 1, ref R, ref G, ref B);
            }

            else if (direction == 1 && isDrawn[y * width + x + 1] && isDrawn[(y - 1) * width + x])
            {
                filterColor(pixelInd + 1, ref R, ref G, ref B);
            }

            else if (direction == 2 && isDrawn[y * width + x - 1] && isDrawn[(y - 1) * width + x])
            {
                filterColor(pixelInd - 1, ref R, ref G, ref B);
            }

            else if (direction == 3 && isDrawn[y * width + x - 1] && isDrawn[(y + 1) * width + x])
            {
                filterColor(pixelInd - 1, ref R, ref G, ref B);
            }
        }

        private byte crrR, crrG, crrB;//global to avoid allocating new variables for every pixel; improvement was next minimal...
        private int toDraw;//they are all used in painPixel()
        private void fillArea(int x, int y, byte R, byte G, byte B)
        {
            //the program may not know where the angles of the newly formed polygons are
            //but it already has the pixels of the edges drawn
            //also, all of the polygons are four sided with no angles greated than 179 deg.
            //all it has to do is draw horisontal lines
            crrR = R; crrG = G; crrB = B;

            toDraw = y * width + x;

            isDrawn[toDraw] = true;//paint the first pixel
            imageData[toDraw * 3] = crrB;
            imageData[toDraw * 3 + 1] = crrG;
            imageData[toDraw * 3 + 2] = crrR;

            int r = x + 1;//draw all the pixels to the right
            for (; r < width && !isDrawn[y * width + r]; r++)
            {
                toDraw = y * width + r;

                isDrawn[toDraw] = true;//every time a method gets called, something slows down the process
                imageData[toDraw * 3] = crrB;
                imageData[toDraw * 3 + 1] = crrG;
                imageData[toDraw * 3 + 2] = crrR;
            }

            int l = x - 1;//draw all the pixels to the left
            for (; l >= 0 && !isDrawn[y * width + l]; l--)
            {
                toDraw = y * width + l;

                isDrawn[toDraw] = true;//I am sure it is explained somewhere
                imageData[toDraw * 3] = crrB;
                imageData[toDraw * 3 + 1] = crrG;
                imageData[toDraw * 3 + 2] = crrR;
            }
            //just because the point under one of the ends of the line is painted, does not mean the next line is drawn
            //the point from where the first line is drawn must be moved along the side of the polygon nad find new empty pixels
            //otherwize, the program will have more polygons to fill, which means more calculations of the first pixels
            int upR = r;
            //look for an unpainted pixel both up and down
            for (int i = l + 1, row = y - 1; row >= 0 && i < upR;)
            {
                if (!isDrawn[row * width + i])//empty pixel found in upper line
                {
                    toDraw = row * width + i;

                    isDrawn[toDraw] = true;//you can replace those rows with painPixel();
                    imageData[toDraw * 3] = crrB;
                    imageData[toDraw * 3 + 1] = crrG;
                    imageData[toDraw * 3 + 2] = crrR;
                    int leftI = i - 1;
                    for (; leftI >= 0 && !isDrawn[row * width + leftI]; leftI--)//draw to the left
                    {
                        toDraw = row * width + leftI;

                        isDrawn[toDraw] = true;//expect a small increase in render time
                        imageData[toDraw * 3] = crrB;
                        imageData[toDraw * 3 + 1] = crrG;
                        imageData[toDraw * 3 + 2] = crrR;
                    }

                    int rightI = i + 1;
                    for (; rightI < width && !isDrawn[row * width + rightI]; rightI++)//draw to the right
                    {
                        toDraw = row * width + rightI;

                        isDrawn[toDraw] = true;//I should also learn how snippets work
                        imageData[toDraw * 3] = crrB;
                        imageData[toDraw * 3 + 1] = crrG;
                        imageData[toDraw * 3 + 2] = crrR;
                    }
                    upR = rightI;

                    row--;//move to the next row
                    i = leftI + 1;//on the next row, start scanning from left most column
                }
                i++;
            }

            int downR = r;
            for (int i = l + 1, row = y + 1; row < height && i < downR;)
            {
                if (!isDrawn[row * width + i])//empty pixel found in upper line
                {
                    toDraw = row * width + i;

                    isDrawn[toDraw] = true;//sadly, C# does not allow macros
                    imageData[toDraw * 3] = crrB;
                    imageData[toDraw * 3 + 1] = crrG;
                    imageData[toDraw * 3 + 2] = crrR;

                    int leftI = i - 1;
                    for (; leftI >= 0 && !isDrawn[row * width + leftI]; leftI--)//draw to the left
                    {
                        toDraw = row * width + leftI;

                        isDrawn[toDraw] = true;
                        imageData[toDraw * 3] = crrB;
                        imageData[toDraw * 3 + 1] = crrG;
                        imageData[toDraw * 3 + 2] = crrR;
                    }

                    int rightI = i + 1;
                    for (; rightI < width && !isDrawn[row * width + rightI]; rightI++)//draw to the right
                    {
                        toDraw = row * width + rightI;

                        isDrawn[toDraw] = true;
                        imageData[toDraw * 3] = crrB;
                        imageData[toDraw * 3 + 1] = crrG;
                        imageData[toDraw * 3 + 2] = crrR;
                    }
                    downR = rightI;

                    row++;//move to the next row
                    i = leftI + 1;//on the next row, start scanning from left most column
                }
                i++;
            }
        }

        private void painPixel()//kept for debugging and demonstration purposes
        {
            isDrawn[toDraw] = true;
            imageData[toDraw * 3] = crrB;
            imageData[toDraw * 3 + 1] = crrG;
            imageData[toDraw * 3 + 2] = crrR;
        }
        private bool isPixelInBoundaries(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        private void paintPixel(int x, int y, byte R, byte G, byte B)//I could keep that one for debugging
        {
            int pixel = y * width + x;

            isDrawn[pixel] = true;

            imageData[pixel * 3] = B;
            imageData[pixel * 3 + 1] = G;
            imageData[pixel * 3 + 2] = R;
        }
        private void pixelColor(int X, int Y, ref byte R, ref byte G, ref byte B)//what would be the color of the pixel if a ray was to pass through it
        {
            R = 255; G = 255; B = 255;

            for (int i = 0; i < lightBlocks.Length; i++)//scan all lines
            {//it could be improved for larger number of light blocks, but I am not expecting that to ever happen
                if (lightPasses(lightsource, new Point(X, Y), lightBlocks[i]))
                {
                    //result = filteredColor(result, lightBlocks[i].color);
                    R = Math.Min(R, lightBlocks[i].color.R);
                    G = Math.Min(G, lightBlocks[i].color.G);
                    B = Math.Min(B, lightBlocks[i].color.B);
                }
            }
        }
        private bool isPixelFilled(int x, int y)//in this program, both checks are always done together
        {
            return !isPixelInBoundaries(x, y) || isDrawn[y * width + x];
        }//I used to use it before trying to lower the number of method calls
        private void filterColor(int pixelInd, ref byte r, ref byte g, ref byte b)//a light's color gets filtered through light blocks
        {
            //if (pixelInd >= isDrawn.Length || pixelInd < 0 || !isDrawn[pixelInd])//from where it is called, it has already been checked if the pixel is drawn and if it is in the screen
            //    return;

            r = Math.Min(r, imageData[pixelInd * 3 + 2]);
            g = Math.Min(g, imageData[pixelInd * 3 + 1]);
            b = Math.Min(b, imageData[pixelInd * 3]);//which colors and intensity can pass through the block
        }
        private bool lightPasses(Point source, Point target, LightBlock lightBlock)
        {
            int A = 0, B = 0, C = 0;

            methods.setEquasion(ref A, ref B, ref C, source, target);

            return//I am only checking what is the color of a pixel and nothing more, so I do not need to know where is the ray reaching
                (A * lightBlock.start.X + B * lightBlock.start.Y + C > 0) != (A * lightBlock.end.X + B * lightBlock.end.Y + C >= 0) &&//both end of the lightBlock are on both sides of the ray
                (lightBlock.A * source.X + lightBlock.B * source.Y + lightBlock.C > 0) != (lightBlock.A * target.X + lightBlock.B * target.Y + lightBlock.C >= 0);//both ray ends are on both sides of the lightBlock
            //in the equasion A * x + B * y + C, when you replace the x and y with the coordinates of a point, with the resulting value you can determine where the point is relative to the line
            //if you have two points, and pass them in the equasion, you can know if they are on different and the same sides from the line
            //if two lines have their ends on both each other's sides, then they intersect
            //here, I only ask if a ray from the light source, that passes through the pixel, passes through a light block
        }
        //private int mouseX = 0, mouseY = 0;
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            lightsource = new Point(e.Location.X, e.Location.Y);
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer.Reset();
            timer.Start();
            redraw();
            timer.Stop();
            label1.Text = timer.ElapsedMilliseconds.ToString();
            //time tracking could be improved
            //also a proper cycle should be implemented
        }
    }

    public static class methods
    {
        public static void setEquasion(ref int A, ref int B, ref int C, Point start, Point end)
        {
            A = start.Y - end.Y;
            B = end.X - start.X;
            C = -1 * B * start.Y - A * start.X;
        }
    }
}

public class UnusedCode
{//deleted code, which I kept if I want to reuse something in the future; you can move it around to see how it would work ot not work at all
    PictureBox pictureBox1;
    private int width, height;
    private bool isPixelFilled(int x, int y) { return false; }
    private void paintPixel(int x, int y, Color color) { }
    bool[] isDrawn;
    byte[] imageData;
    private void fillAreaBFS(int x, int y, Color color)
    {
        if (x >= width || x < 0 ||
            y >= height || y < 0 ||
            isDrawn[y * width + x])
            return;

        List<Point> pixelList = new List<Point>();//maybe I shouldn't have used a list, but I doubt that handling a data structure would be faster than scanning all the pixels
        pixelList.Add(new Point(x, y));

        for (int i = 0; i < pixelList.Count && i < 100; i++)
        {
            paintPixel(x, y, color);

            if (isPixelFilled(x + 1, y)) pixelList.Add(new Point(x + 1, y));
            if (isPixelFilled(x - 1, y)) pixelList.Add(new Point(x - 1, y));
            if (isPixelFilled(x, y + 1)) pixelList.Add(new Point(x, y + 1));
            if (isPixelFilled(x, y - 1)) pixelList.Add(new Point(x, y - 1));
        }
    }
    private void fillAreaRec(int x, int y, byte R, byte G, byte B)
    {//causes a StackOverflow long before it manages to scan through a polygon; also, it requires 4 mrthod calls per pixel
        int pixel = y * width + x;
        if (x >= width || x < 0 || y >= height || y < 0 || isDrawn[pixel])
            return;

        imageData[pixel * 3] = B;
        imageData[pixel * 3 + 1] = G;
        imageData[pixel * 3 + 2] = R;
        isDrawn[pixel] = true;

        fillAreaRec(x + 1, y, R, G, B);//next column
        fillAreaRec(x - 1, y, R, G, B);//previous column
        fillAreaRec(x, y + 1, R, G, B);//next row
        fillAreaRec(x, y - 1, R, G, B);//previous row
                                       //needs to include a case for when the pixel is on a fist/last rolw/column
    }
    private void pixelColor(int x, int y, ref byte R, ref byte G, ref byte B) { }
    private bool isPixelDrawn(int x, int y) { return false; }
    private void paintPixel(int x, int y, byte R, byte G, byte B) { }
    private void fillArea(int x, int y)
    {//the first version of the current method; it cannot fill whole polygons, requiring to start the process more times
        if (isDrawn[y * width + x])
            return;

        //Color color = pixelColor(x, y);
        byte R = 0, G = 0, B = 0;
        pixelColor(x, y, ref R, ref G, ref B);

        int left = x;
        int right = x + 1;

        int up = y - 1;
        int down = y + 1;

        for (; left >= 0 && !isPixelDrawn(left, y);)
        {
            paintPixel(left, y, R, G, B);

            for (; up >= 0 && !isPixelDrawn(left, up);)
            {
                paintPixel(left, up, R, G, B);
                up--;
            }

            for (; down < height && !isPixelDrawn(left, down);)
            {
                paintPixel(left, down, R, G, B);
                down++;
            }

            left--;
            up = y - 1;
            down = y + 1;
        }

        up = y - 1;
        down = y + 1;
        for (; right < width && !isPixelDrawn(right, y);)
        {
            paintPixel(right, y, R, G, B);

            for (; up >= 0 && !isPixelDrawn(right, up);)
            {
                paintPixel(right, up, R, G, B);
                up--;
            }

            for (; down < height && !isPixelDrawn(right, down);)
            {
                paintPixel(right, down, R, G, B);
                down++;
            }

            right++;
            up = y - 1;
            down = y + 1;
        }
    }
    private void fillArea(int x, int y, byte R, byte G, byte B)
    {//another recursive attempt - this time draws lines in a different direction once it hits a polygon edge; it can't fill one in one scan and also does not work for some reason
        if (isPixelFilled(x, y))
            return;

        paintPixel(x, y, R, G, B);

        int i = 1;

        for (; x + i < width && !isPixelFilled(x + i, y); i++)//to the right
            paintPixel(x + i, y, R, G, B);

        fillArea(x + i - 1, y + 1, R, G, B);
        fillArea(x + i - 1, y - 1, R, G, B);

        i = 1;
        for (; x - i >= 0 && !isPixelFilled(x - i, y); i++)//to the left
            paintPixel(x - i, y, R, G, B);

        fillArea(x - i + 1, y + 1, R, G, B);
        fillArea(x - i + 1, y - 1, R, G, B);

        i = 1;
        for (; y + i < height && !isPixelFilled(x, y + i); i++)//to the down
            paintPixel(x, y + i, R, G, B);

        fillArea(x + 1, y + i - 1, R, G, B);
        fillArea(x - 1, y + i - 1, R, G, B);

        i = 1;
        for (; y - i >= 0 && !isPixelFilled(x, y - i); i++)//to the up
            paintPixel(x, y - i, R, G, B);

        fillArea(x + 1, y - i + 1, R, G, B);
        fillArea(x - 1, y - i + 1, R, G, B);
    }
    private Color getPixelColor(int pixelIndex)
    {
        return Color.FromArgb(
            imageData[pixelIndex * 3 + 2],
            imageData[pixelIndex * 3 + 1],
            imageData[pixelIndex * 3]);
    }
    private void getPixelColor(int pixelIndex, ref byte r, ref byte g, ref byte b)
    {
        r = imageData[pixelIndex * 3 + 2];
        g = imageData[pixelIndex * 3 + 1];
        b = imageData[pixelIndex * 3];
    }

    private class LightBlock//put to prevent errors from occuring
    {
        public Point start;
        public Point end;
        public Color color;
    }
    private List<Point> debugPoints;
    private List<Color> debugColors;
    private void addDebugLine(LightBlock line)
    {
        debugPoints.Add(line.start);
        debugColors.Add(line.color);
        debugPoints.Add(line.end);
        debugColors.Add(line.color);
    }
    private void testLineDrawing()
    {
        Graphics gfx = Graphics.FromImage(pictureBox1.Image);
        Pen pen = new Pen(Color.Black);

        int dx = 500;
        int dy = -100;

        int yi = 1;//slope
        if (dy < 1) { yi = -1; dy = -dy; }

        int D = 2 * dy - dx;
        int y = 0;

        for (int x = 0; x < 500; x++)
        {
            gfx.DrawLine(pen, x + 200, y + 200, x + 200 + 1, y + 200 + 1);
            if (D > 0)
            {
                y = y + yi;
                D = D + (2 * (dy - dx));
            }
            else
                D = D + 2 * dy;
        }
    }
    private void addDebugPoint(Point point, Color color)
    {
        debugColors.Add(color);
        debugPoints.Add(point);
    }
}