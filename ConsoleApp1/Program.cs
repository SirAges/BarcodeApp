using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleApp1
{
    public class Program
    {

//----------FUNCTION-----------

      
        //-------------END FUNCTION-------------

       
        static void Main(string[] args)
        {

            Rectangle rect1;
            rect1.length = 200;
            rect1.width = 300;
            Console.WriteLine("Area of rectangele is {0}", rect1.Area());

            Rectangle rect2 = new Rectangle(300, 40);

            Console.WriteLine("Area of rectangele is {0}", rect2.Area());

            rect2 = rect1;
            rect1.length = 23; Console.WriteLine("Area of rectangele is {0}", rect2.length);

            Animal fox = new Animal()
            {
                name = "Red",
                sound="Raaw"
            };

            Console.WriteLine("num of animals is {0}", Animal.GetNumAnimals());


            Console.WriteLine("Area of rectangle is {0}", ShapeMath.GetArea("rectangle", 5, 6));

        }

        struct Rectangle
        {
            public double width;
            public double length;

            public Rectangle(double l = 1, double w = 1)
            {
                length = l;
                width = w;
            }

            public double Area()
            {
                return length * width;
            }
        }


    }
}