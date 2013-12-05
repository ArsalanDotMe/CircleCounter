using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinCounter;
using System.Drawing;
namespace CC_tester
{
    class Program
    {
        static void Main(string[] args)
        {
            CoinCounter.CoinCounter CC = new CoinCounter.CoinCounter();
            
            Bitmap B1 = new Bitmap(@"C:\Users\Arsalan\Desktop\CV test\1.jpg");
            Bitmap B2 = new Bitmap(@"C:\Users\Arsalan\Desktop\CV test\3.jpg");
            CC.tolerance = 0.7;
            CC._min_radius = 15;
            var coins = CC.GetCoins(B2);
            CC.DrawCoins(coins, B2, "output.bmp", System.Drawing.Imaging.ImageFormat.Bmp, Color.Red);
            Console.Clear();
            foreach (Coin item in coins)
            {
                Console.WriteLine("xpos: {0}, ypos: {1}, radius: {2}", item.xpos, item.ypos, item.radius);
            }
        }
    }
}
