using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinCounter
{
    public class CoinCounter
    {

        public int _min_radius = 5;
        public double tolerance = 1;
        public int minimum_circle_distance = 10;
        public int clean_tolerance = 15;
        public int clean_level = 3;
        private double round_off(double number, int points)
        {
            return (double)Math.Round((decimal)number, points);
        }

        private bool within_tolerance(double some_number)
        {
            if (some_number >= 0 && some_number < tolerance)
                return true;
            else return false;
        }

        private void enhance_mask(ref int[,] mask, int width, int height, int zero_threshold)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (mask[y, x] == 1)
                    {
                        continue;
                    }
                    int zcount = 0, ocount = 0;
                    for (int cy = Math.Max(0, y - 2); cy <= Math.Min(y + 2, height - 1); cy++)
                    {
                        for (int cx = Math.Max(0, x - 2); cx <= Math.Min(x + 2, width - 1); cx++)
                        {
                            if (cx == x && cy == y)
                            {
                                continue;
                            }
                            if (mask[cy, cx] == 0)
                            {
                                zcount++;
                            }
                            else
                            {
                                ocount++;
                            }
                        }
                    }
                    if (zcount <= 2)
                    {
                        mask[y, x] = 1;
                    }
                }
            }
        }

        public void DrawCoins(List<Coin> coins, Bitmap existing_image, string outputname, System.Drawing.Imaging.ImageFormat format, Color coin_color)
        {
            Bitmap out_bmp = new Bitmap(existing_image);
            Graphics gr = Graphics.FromImage(out_bmp);
            Pen p = new Pen(coin_color);
            foreach (Coin coin in coins)
            {
                gr.DrawEllipse(p, coin.xpos - coin.radius, coin.ypos - coin.radius, coin.radius*2, coin.radius*2);
            }
            out_bmp.Save(outputname, format);
        }

        public List<Coin> GetCoins(Bitmap bmp_image)
        {
            int m_c_d_sq = minimum_circle_distance * minimum_circle_distance;
            List<Coin> detectedCoins = new List<Coin>();
            Bitmap clone_bmp = (Bitmap)bmp_image.Clone();
            Bitmap working_image = clone_bmp;
            int height = working_image.Height;
            int width = working_image.Width;

            int[,] back_fore_mask = new int[height,width];
            LABColor white = xyz_to_lab(rgb_to_xyz(Color.White));
            
            Console.WriteLine("Generating map...");
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    if (de_1994(xyz_to_lab(rgb_to_xyz(working_image.GetPixel(w,h))),white) < tolerance)
                    {
                        back_fore_mask[h, w] = 0;
                    }
                    else
                    {
                        back_fore_mask[h, w] = 1;
                    }
                }
            }


            enhance_mask(ref back_fore_mask, width, height, 2);

            Bitmap maskBitmap = new Bitmap(width, height);
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    if (back_fore_mask[h, w] == 0)
                    {
                        maskBitmap.SetPixel(w, h, Color.Black);
                    }
                    else
                    {
                        maskBitmap.SetPixel(w, h, Color.White);
                    }
                }
            }
            maskBitmap.Save("mask.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

            Console.WriteLine("Map Generated! Finding circles...");

            int max_radius = Math.Min(height, width);
            int radius = _min_radius;
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    //var circlePixels = get_pixels_in_circle(col, row, radius, working_image);
                    //double SD = get_color_standard_deviation(circlePixels);
                    int zero_count, one_count;

                    if (back_fore_mask[row, col] == 0)
                        continue;

                    count_pixels_in_circle(col,row,radius,back_fore_mask,width,height,out zero_count,out one_count);

                    if (zero_count == 0)
                    {
                        bool ignore_this = false;

                        bool wiggleRoom = true;
                        bool lockX = false, lockY = false;
                        int tradius = radius, tx = col, ty = row;

                        while (zero_count == 0)
                        {
                            foreach (Coin detected_coin in detectedCoins)
                            {
                                int dx_sq = (tx-detected_coin.xpos) * (tx-detected_coin.xpos);
                                int dy_sq = (ty-detected_coin.ypos) * (ty-detected_coin.ypos);
                                if (dx_sq + dy_sq <= detected_coin.radius * detected_coin.radius)
                                {
                                    ignore_this = true;
                                    break;
                                }
                            }
                            if (ignore_this)
                                break;

                            while (wiggleRoom)
                            {
                                int Zup, Zdown, Zleft, Zright;
                                count_pixels_in_circle(tx, ty, tradius, back_fore_mask, width, height, out zero_count, out one_count);
                                if (zero_count == 0)
                                    break;
                                //if ((double)zero_count / (double)one_count > 0.01)
                                //{
                                //    ignore_this = true;
                                //    break;
                                //}
                                    

                                //wiggle down
                                count_pixels_in_circle(tx, ty + 1, tradius, back_fore_mask, width, height, out Zdown, out one_count);
                                if (Zdown == 0)
                                {
                                    ty++;
                                    lockX = false;
                                }
                                else
                                {
                                    count_pixels_in_circle(tx, ty - 1, tradius, back_fore_mask, width, height, out Zup, out one_count);
                                    if (Zup == 0)
                                    {
                                        ty--;
                                        lockX = false;
                                    }
                                    else
                                    {
                                        if (Zdown>=zero_count && Zup>= zero_count)
                                        {
                                            lockY = true;
                                        }
                                        else
                                        {
                                            lockX = false;
                                            if (Math.Min(Zdown, Zup) == Zdown)
                                            {
                                                ty++;
                                            }
                                            else
                                            {
                                                ty--;
                                            }
                                        }
                                    }
                                }

                                count_pixels_in_circle(tx, ty, tradius, back_fore_mask, width, height, out zero_count, out one_count);
                                if (zero_count == 0)
                                    break;

                                count_pixels_in_circle(tx + 1, ty, tradius, back_fore_mask, width, height, out Zright, out one_count);
                                if (Zright == 0)
                                {
                                    tx++;
                                    lockY = false;
                                }
                                else
                                {
                                    count_pixels_in_circle(tx - 1, ty, tradius, back_fore_mask, width, height, out Zleft, out one_count);
                                    if (Zleft == 0)
                                    {
                                        tx--;
                                        lockY = false;
                                    }
                                    else
                                    {
                                        if (Zleft >= zero_count && Zright >= zero_count)
                                        {
                                            lockX = true;
                                        }
                                        else
                                        {
                                            lockY = false;
                                            if (Math.Min(Zleft, Zright) == Zleft)
                                            {
                                                tx--;
                                            }
                                            else
                                            {
                                                tx++;
                                            }
                                        }
                                    }
                                }
                                if (lockX == true && lockY == true)
                                {
                                    wiggleRoom = false;
                                }
                                
                            }
                            tradius++;
                        }
                        if (!ignore_this)
                        {
                            detectedCoins.Add(new Coin { radius = tradius - 1, xpos = tx, ypos = ty });
                            Console.WriteLine("xpos: {0}, ypos: {1}, radius: {2}", tx, ty, tradius);
                        }
                    }
                }
            }
            return detectedCoins;
        }

        private List<Coin> cleaner_driver(List<Coin> coins_list , int tolerance, int levels)
        {
            List<Coin> cleaned_list = coins_list;
            for (int i = 0; i < levels; i++)
            {
                cleaned_list = clean_results(cleaned_list, tolerance);
            }
            return cleaned_list;
        }

        private List<Coin> clean_results(List<Coin> coins_list , int tolerance)
        {
            List<Coin> cleaned_result = new List<Coin>();
            bool close_found = true;
            while(close_found && coins_list.Count > 0)
            {
                Coin first = coins_list[0];
                int xdis = 0;
                int ydis = 0;
                List<Coin> close_to_this = new List<Coin>();
                foreach (Coin other_coins in coins_list)
                {
                    xdis = Math.Abs(first.xpos - other_coins.xpos);
                    ydis = Math.Abs(first.ypos - other_coins.ypos);
                    if (xdis < tolerance && ydis < tolerance)
                    {
                        close_to_this.Add(other_coins);
                    }
                }

                float max_radius = 0;
                foreach (Coin item in close_to_this)
                {
                    if (item.radius > max_radius)
                        max_radius = item.radius;
                }
                Coin with_max_rad = null;
                foreach (Coin item in close_to_this)
                {
                    if (item.radius == max_radius)
                    {
                        with_max_rad = item;
                        break;
                    }
                }

                if (with_max_rad != null)
                    cleaned_result.Add(with_max_rad);

                foreach (Coin item in close_to_this)
                {
                    coins_list.Remove(item);
                }
            }

            return cleaned_result;
        }

        private double get_color_standard_deviation(List<Color> colorful_array)
        {
            double L_sum = 0;
            double A_sum = 0;
            double B_sum = 0;

            List<LABColor> lab_colors = new List<LABColor>();

            int c_a_len = colorful_array.Count;
            for (int i = 0; i < c_a_len; i++)
            {
                LABColor tempc = xyz_to_lab(rgb_to_xyz(colorful_array[i]));
                L_sum += tempc.L;
                A_sum += tempc.A;
                B_sum += tempc.B;

                lab_colors.Add(tempc);
            }

            double L_average = L_sum / c_a_len;
            double A_average = A_sum / c_a_len;
            double B_average = B_sum / c_a_len;

            LABColor mean_color = new LABColor { L = L_average, A = A_average, B = B_average };

            double sum_of_variance_squares = 0;

            foreach (var color in lab_colors)
            {
                sum_of_variance_squares += Math.Pow((de_1994(mean_color, color)), 2);
            }
            double variance = round_off(sum_of_variance_squares / (double)c_a_len, 5);
            double SD = round_off(Math.Sqrt(variance), 3);
            if (Double.IsNaN(SD))
            {
                throw new Exception("Double is NaN!");
            }
            return SD;
        }

        private bool is_background(Color pixel, int tolerance)
        {
                LABColor tempc = xyz_to_lab(rgb_to_xyz(pixel));

            var whiteCol = new LABColor { L = 100.0, A = 23.198503, B = 172.4138 };
            if (de_1994(tempc, whiteCol) < 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool is_background(List<Color> colorful_array, int tolerance)
        {
            double L_sum = 0;
            double A_sum = 0;
            double B_sum = 0;

            List<LABColor> lab_colors = new List<LABColor>();

            int c_a_len = colorful_array.Count;
            for (int i = 0; i < c_a_len; i++)
            {
                LABColor tempc = xyz_to_lab(rgb_to_xyz(colorful_array[i]));
                L_sum += tempc.L;
                A_sum += tempc.A;
                B_sum += tempc.B;

                lab_colors.Add(tempc);
            }

            double L_average = L_sum / c_a_len;
            double A_average = A_sum / c_a_len;
            double B_average = B_sum / c_a_len;

            LABColor mean_color = new LABColor { L = L_average, A = A_average, B = B_average };
            var whiteCol = new LABColor { L = 100.0, A = 23.198503, B = 172.4138 };
            if (de_1994(mean_color, whiteCol) < 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private int[] get_pixels_in_circle(int coord_x, int coord_y, int radius, int[,] back_fore_mask, int width, int height)
        {
            List<int> contained_bits = new List<int>();
            int radius_square = radius * radius;
            //int square_length = radius * 2;
            int max_y_limit = Math.Min(coord_y + radius, height);
            int max_x_limit = Math.Min(coord_x + radius, width);
            for (int y = Math.Max(coord_y - radius, 0); y < max_y_limit; y++)
            {
                for (int x = Math.Max(coord_x - radius, 0); x < max_x_limit; x++)
                {
                    int y_diff = (y - coord_y);
                    int x_diff = (x - coord_x);

                    if (y_diff * y_diff + x_diff * x_diff <= radius_square)
                    {
                        contained_bits.Add(back_fore_mask[y,x]);
                    }
                }
            }
            return contained_bits.ToArray();
        }
        private void count_pixels_in_circle(int coord_x, int coord_y, int radius, int[,] back_fore_mask, int width, int height, out int zeroes, out int ones)
        {
            zeroes = 0;
            ones = 0;
            int radius_square = radius * radius;
            int max_y_limit = Math.Min(coord_y + radius, height);
            int max_x_limit = Math.Min(coord_x + radius, width);
            for (int y = Math.Max(coord_y - radius, 0); y < max_y_limit; y++)
            {
                for (int x = Math.Max(coord_x - radius, 0); x < max_x_limit; x++)
                {
                    int y_diff = (y - coord_y);
                    int x_diff = (x - coord_x);

                    if (y_diff * y_diff + x_diff * x_diff <= radius_square)
                    {
                        if (back_fore_mask[y, x] == 0)
                        {
                            zeroes++;
                        }
                        else if (back_fore_mask[y,x] == 1)
                        {
                            ones++;
                        }
                    }
                }
            }
        }

        private List<Color> get_pixels_in_circle(int coord_x, int coord_y, int radius, Bitmap bmp)
        {
            List<Color> pixels_list = new List<Color>();
            int radius_square = radius * radius;
            //int square_length = radius * 2;
            int max_y_limit = Math.Min(coord_y + radius, bmp.Height);
            int max_x_limit = Math.Min(coord_x + radius, bmp.Width);
            for (int y = Math.Max(coord_y - radius, 0); y < max_y_limit; y++)
            {
                for (int x = Math.Max(coord_x - radius, 0); x < max_x_limit; x++)
                {
                    int y_diff = (y - coord_y);
                    int x_diff = (x - coord_x);

                    if (y_diff * y_diff + x_diff * x_diff < radius_square)
                    {
                        pixels_list.Add(bmp.GetPixel(x, y));
                    }
                }
            }
            return pixels_list;
        }

        private XYZColor rgb_to_xyz(Color rgb_col)
        {
            var red = (double)rgb_col.R;
            var green = (double)rgb_col.G;
            var blue = (double)rgb_col.B;

            var _red = red / 255;
            var _green = green / 255;
            var _blue = blue / 255;

            if (_red > 0.04045)
            {
                _red = (_red + 0.055) / 1.055;
                _red = Math.Pow(_red, 2.4);
            }
            else
            {
                _red = _red / 12.92;
            }

            if (_green > 0.04045)
            {
                _green = (_green + 0.055) / 1.055;
                _green = Math.Pow(_green, 2.4);
            }
            else
            {
                _green = _green / 12.92;
            }

            if (_blue > 0.04045)
            {
                _blue = (_blue + 0.055) / 1.055;
                _blue = Math.Pow(_blue, 2.4);
            }
            else
            {
                _blue = _blue / 12.92;
            }

            _red *= 100;
            _green *= 100;
            _blue *= 100;

            XYZColor converted = new XYZColor();
            converted.x = _red * 0.4124 + _green * 0.3576 + _blue * 0.1805;
            converted.y = _red * 0.2126 + _green * 0.7152 + _blue * 0.0722;
            converted.x = _red * 0.0193 + _green * 0.1192 + _blue * 0.9505;

            return converted;
        }

        private LABColor xyz_to_lab(XYZColor xyz_col)
        {
            var x = xyz_col.x;
            var y = xyz_col.y;
            var z = xyz_col.z;

            var _x = x / 95.047;
            var _y = y / 100;
            var _z = z / 108.883;

            if (_x > 0.008856)
            {
                _x = Math.Pow(_x, 1.0d / 3.0d);
            }
            else
            {
                _x = 7.787 * _x + 16.0d / 116.0d;
            }

            if (_y > 0.008856) { _y = Math.Pow(_y, 1.0d / 3.0d); }
            else { _y = 7.787 * _y + 16.0d / 116.0d; }

            if (_z > 0.008856) { _z = Math.Pow(_z, 1.0d / 3.0d); }
            else { _z = 7.787 * _z + 16.0d / 116.0d; }

            var l = 116 * _y - 16.0d;
            var a = 500 * (_x - _y);
            var b = 200 * (_y - _z);
            return new LABColor { L = l, A = a, B = b };
        }

        private double de_1994(LABColor lab1, LABColor lab2)
        {
            var c1 = round_off(Math.Sqrt(lab1.A * lab1.A + lab1.B * lab1.B), 4);
            var c2 = round_off(Math.Sqrt(lab2.A * lab2.A + lab2.B * lab2.B), 4);

            var dc = c1 - c2;

            var dl = round_off(lab1.L - lab2.L, 5);
            var da = round_off(lab1.A - lab2.A, 5);
            var db = round_off(lab1.L - lab2.L, 5);

            var dh = (da * da) + (db * db) - (dc * dc);
            if (dh < 0)
            {
                dh = 0;
            }
            else
            {
                dh = Math.Sqrt(dh);
            }
            var first = dl;
            var second = dc / (1 + 0.045 * c1);
            var third = dh / (1 + 0.015 * c1);

            double delta = (Math.Sqrt(first * first + second * second + third * third));
            if (Double.IsNaN(delta))
            {
                throw new Exception("Double is NaN!");
            }
            return delta;
        }

    }
}
