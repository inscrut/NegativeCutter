using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NegativeCutter
{
    class Program
    {
        private enum ArgType
        {
            info, type, outfile, outdir, w, h, none, error
        }

        static void Main(string[] args)
        {
            #region variables
            ArgType arg_type = ArgType.none;
            string path_infofile = "info.txt";
            string path_outfile = "Bad.dat";
            string path_outdir = "Bad";
            string type_file = "bmp";
            int score = 0;
            Size cutter = new Size(64, 64);
            HitBox hitbox = new HitBox();
            GImg trg_img;
            #endregion

            #region getargs
            bool _type = false;
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-info": arg_type = ArgType.info; break;
                    case "-outfile": arg_type = ArgType.outfile; break;
                    case "-outdir": arg_type = ArgType.outdir; break;
                    case "-w": arg_type = ArgType.w; break;
                    case "-h": arg_type = ArgType.h; break;
                    case "-type": arg_type = ArgType.type; break;
                    default: _type = true; break;
                }
                if (!_type) continue;
                else
                {
                    switch (arg_type)
                    {
                        case ArgType.info: path_infofile = arg; break;
                        case ArgType.w: cutter.Width = Convert.ToInt32(arg); break;
                        case ArgType.h: cutter.Height = Convert.ToInt32(arg); break;
                        case ArgType.outfile: path_outfile = arg; break;
                        case ArgType.outdir: path_outdir = arg; break;
                        case ArgType.type: type_file = arg; break;
                        default: arg_type = ArgType.error; break;
                    }
                    if (arg_type != ArgType.error) arg_type = ArgType.none;
                    else
                    {
                        Console.WriteLine("Invalid arg: {0}\r\nAbort.", arg);
                        Environment.Exit(1);
                    }
                }
                _type = false;
            }
            #endregion

            #region check
            if (!File.Exists(path_infofile))
            {
                Console.WriteLine("ERROR: Info file {0} not found!", path_infofile);
                Environment.Exit(1);
            }
            if (!Directory.Exists(path_outdir))
            {
                Console.WriteLine("Directory {0} not exists! Creating...", path_outdir);
                Directory.CreateDirectory(path_outdir);
            }
            if (!File.Exists(path_outfile))
            {
                Console.WriteLine("Out file {0} not exists! Creating...", path_outfile);
                using (File.Create(path_outfile)) { }
            }
            else if (File.Exists(path_outfile))
            {
                using (File.Create(path_outfile)) { }
            }
            if(Directory.GetFiles(path_outdir).Length > 1)
            {
                Console.WriteLine("Warning! '{0}' path is not empty!\r\nPress ENTER to continue or Ctrl+C for exit...", path_outdir);
                Console.Read();
            }
            #endregion

            hitbox.coord = new Point(0, 0);
            hitbox.size = cutter;

            Console.WriteLine("Starting cut images...\r\n");
            foreach (var trg_line in File.ReadAllLines(path_infofile))
            {
                trg_img = new GImg(trg_line);
                Console.WriteLine("<-- {0}", trg_img.path);
                using (Image global_bmp = Image.FromFile(trg_img.path))
                {
                    List<HitBox> boxes = new List<HitBox>();
                    for (int i = 0; i < global_bmp.Size.Width / hitbox.size.Width; i++)
                    {
                        for (int j = 0; j < global_bmp.Size.Height / hitbox.size.Height; j++)
                        {
                            hitbox.coord.X = i * hitbox.size.Width;
                            hitbox.coord.Y = j * hitbox.size.Height;

                            if (!checkHitbox(trg_img, hitbox)) continue;

                            boxes.Add(hitbox);
                        }
                    }

                    drawHitBoxes(global_bmp, boxes, trg_img.path, type_file);

                    int lnum = getLastNum(path_outdir);
                    int diff = lnum;
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(path_outfile, true))
                        {
                            foreach (var box in boxes)
                            {
                                string _tmp_path = path_outdir + @"\" + "neg" + lnum.ToString() + "." + type_file;
                                Crop(global_bmp, new Rectangle(box.coord, box.size), _tmp_path, type_file);

                                sw.WriteLine(_tmp_path);
                                lnum++;
                            }
                            sw.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERROR: {0}", e.Message);
                        Environment.Exit(1);
                    }
                    Console.WriteLine("    - {0} images has been croped!", lnum - diff);
                    score += lnum - diff;
                }
            }
            Console.WriteLine("\r\nWas made {0} pictures!", score);
            Console.WriteLine("\r\nDone.");
        }

        public struct GImg
        {
            public string path;

            public List<Target> imgs;

            public GImg(string line)
            {
                string[] _args = line.Split(' ');
                path = _args[0];

                imgs = new List<Target>();
                Target imgobj = new Target();

                short s = 0;
                short val = 0;

                foreach (var item in _args)
                {
                    s++;
                    if (s <= 2) continue;

                    switch (val)
                    {
                        case 0: imgobj.pos.X = Convert.ToInt32(item); break;
                        case 1: imgobj.pos.Y = Convert.ToInt32(item); break;
                        case 2: imgobj.size.Width = Convert.ToInt32(item); break;
                        case 3: imgobj.size.Height = Convert.ToInt32(item); break;
                        default: break;
                    }
                    val++;
                    if (val > 3) { val = 0; imgs.Add(imgobj); imgobj = new Target(); }
                }
            }
        }

        public struct Target
        {
            public Point pos;
            public Size size;
        }

        public struct HitBox
        {
            public Point coord;
            public Size size;
        }

        static int getLastNum(string _path)
        {
            return Directory.GetFiles(_path).Length+1;
        }

        static bool checkHitbox(GImg _img, HitBox _hb)
        {
            Point[] hb_points = {
                _hb.coord,
                new Point(_hb.coord.X + _hb.size.Width, _hb.coord.Y),
                new Point(_hb.coord.X, _hb.coord.Y + _hb.size.Height),
                new Point(_hb.coord.X + _hb.size.Width, _hb.coord.Y + _hb.size.Height)
            };
            foreach (var pbox in _img.imgs)
            {
                Point[] p_points = {
                    pbox.pos,
                    new Point(pbox.pos.X + pbox.size.Width, pbox.pos.Y),
                    new Point(pbox.pos.X, pbox.pos.Y + pbox.size.Height),
                    new Point(pbox.pos.X + pbox.size.Width, pbox.pos.Y + pbox.size.Height)
                };

                foreach (var hbpoint in hb_points)
                    if ((hbpoint.X >= pbox.pos.X && hbpoint.X <= pbox.pos.X + pbox.size.Width) && (hbpoint.Y >= pbox.pos.Y && hbpoint.Y <= pbox.pos.Y + pbox.size.Height))
                        return false;
                foreach (var p_point in p_points)
                    if ((p_point.X >= _hb.coord.X && p_point.X <= _hb.coord.X + _hb.size.Width) && (p_point.Y >= _hb.coord.Y && p_point.Y <= _hb.coord.Y + _hb.size.Height))
                        return false;
                p_points = null;
            }
            hb_points = null;

            return true;
        }

        static void drawHitBoxes(Image _global, List<HitBox> _boxes, string _path, string _type)
        {
            using(Image res = new Bitmap(_global))
            {
                using (Graphics g = Graphics.FromImage(res))
                {
                    foreach (var box in _boxes)
                        g.DrawRectangle(new Pen(new SolidBrush(Color.Red)), new Rectangle(box.coord, box.size));
                    res.Save(_path + ".result." + _type, System.Drawing.Imaging.ImageFormat.Bmp);
                    Console.WriteLine("--> {0}", _path + ".result." + _type);
                }
            }           
        }

        static void Crop(Image _image, Rectangle selection, string _path, string _type)
        {
            using (Bitmap buff = new Bitmap(_image))
                switch (_type)
                {
                    case "bmp":
                        buff.Clone(selection, buff.PixelFormat).Save(_path, System.Drawing.Imaging.ImageFormat.Bmp);
                        break;
                    case "png":
                        buff.Clone(selection, buff.PixelFormat).Save(_path, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    case "jpg":
                        buff.Clone(selection, buff.PixelFormat).Save(_path, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                    default:
                        break;
                }
        }
    }
}
