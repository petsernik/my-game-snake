using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static System.Math;

namespace WindowsFormsApp3
{
    public partial class Game : Form
    {
        static bool error = true;
        List<int> parameters = new List<int>();
        List<int> get_param(string from)
        {
            List<int> get = new List<int>();
            List<int> str = new List<int>();
            for (int i = 0; i < from.Length; ++i) str.Add(from[i] - 48);
            int x = 0, y = 1;
            while (str.Count > 0)
            {
                if (str[0] == ' ' - 48)
                {
                    str.RemoveAt(0);
                    get.Add(x * y);
                    x = 0; y = 1;
                }
                else if (str[0] == '-' - 48)
                {
                    y = -1;
                    str.RemoveAt(0);
                }
                else
                {
                    x *= 10;
                    x += str[0];
                    str.RemoveAt(0);
                }
            }
            return get;
        }
        public static ref Game form()
        {
            return ref Init.reform.form;
        }
        public static void time(ref Timer timer, in int Interval, in EventHandler Event)
        {
            timer.Interval = Interval;
            timer.Tick += Event;
            timer.Start();
        }
        void swap<T>(ref T a, ref T b)
        {
            T medium = a;
            a = b;
            b = medium;
        }

        public Game()
        {
            InitializeComponent();
            try
            {
                Initialize();
                Start(); Cursor.Show(); cursor = 1;
            }
            catch
            {
                if (File.Exists("parameters.txt"))
                {
                    File.Delete("parameters.txt");
                    Initialize();
                    Start(); Cursor.Show(); cursor = 1;
                }
            }
        }
        void Initialize()
        {
            // parameters
            {
                string orig = $"35 1 5 1 6 4 20 {int.MaxValue} 1 0 0 0 0 0 {Color.Red.ToArgb()} {Color.Blue.ToArgb()} 1 ";
                if (!File.Exists("parameters.txt")) File.WriteAllText("parameters.txt", orig);
                string from = File.ReadAllText("parameters.txt");
                parameters = get_param(from);
                rectangle.width = rectangle.height = parameters[0];
                field.players = parameters[1]; field.max_food_count = parameters[2]; field.max_arm_count = parameters[3]; field.max_speed_count = parameters[4];
                snake.v0 = parameters[5]; field.mushroom = parameters[6]; snake.finish = parameters[7]; snake.acceleration = parameters[8];
                field.param = new bool[5];
                for (int i = 0; i < 5; ++i) field.param[i] = parameters[9 + i] == 1;
                field.bonus_snakes = field.param[0] ? 1 : 0;
                field.colors = new Color[2] { Color.FromArgb(parameters[14]), Color.FromArgb(parameters[15]) };
                field.creating_mode = parameters[16];
            }

            // client
            {
                ClientSize = new Size(monitor.width(), monitor.height());
                FormBorderStyle = FormBorderStyle.None;
            }

            // form
            {
                KeyDown += Form1_KeyDown;
                FormClosed += Form1_Closed;
                Text = "Python";

                main.create(new string[] { "Старт", "Настройки", "Инфо", "Выйти" });
                for (int i = 0; i < main.length; ++i) Controls.Add(main.buttons[i]);

                string[] names = new string[] 
                { " Хотите грибы? ", " Поменять WASD на стрелки первому игроку? ", " Показывать счёт? ", "  Больше настроек! ", " Вернуться в меню" };
                settings.create(field.param, names);
                for (int i = 0; i < settings.length; ++i) Controls.Add(settings.check[i]);
                settings.hide("CheckBox");

                names = new string[]
                {
                    $"Длина единичной змеи\n (больше 15, меньше {Min(monitor.height(),monitor.width())})",
                    "Число игроков\n (не больше двух)", "Количество еды на поле",
                    "Количество брони на поле", "Число бонусов скорости на поле",
                    "Начальная скорость змеи (делитель 1000)",
                    "Начальное число грибов", $"Победное число \n(не больше {int.MaxValue})",
                    "Режим ускорения\n (от 0 до 3)"
                };
                settingsAddTexts(names);
                settingsAddCircleColors();
                settings.hide("texts");
            }

            // form_events
            void Form1_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.F5) { Clear(); Start(); }
                else if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) { main.show(); Cursor.Show(); cursor = 1; }
                else if (e.KeyCode == Keys.ControlKey) form().CursorChange();
            }
            void Form1_Closed(object sender, EventArgs e)
            {
                if (!error)
                {
                    string save = "";
                    parameters = new List<int>()
                    {
                        rectangle.width, field.players, field.max_food_count, field.max_arm_count, field.max_speed_count,
                        snake.v0, field.mushroom, snake.finish, snake.acceleration,
                        field.param[0] ? 1 : 0, field.param[1] ? 1 : 0, field.param[2] ? 1 : 0, field.param[3] ? 1 : 0, field.param[4] ? 1 : 0,
                        field.colors[0].ToArgb(), field.colors[1].ToArgb(), field.creating_mode
                    };
                    for (int i = 0; i < parameters.Count; ++i) save += parameters[i].ToString() + " ";
                    File.WriteAllText("parameters.txt", save);
                }
                else if (File.Exists("parameters.txt")) { File.Delete("parameters.txt"); Application.Restart(); }
            }
        }
        void Start()
        {
            Cursor.Hide(); cursor = 0;

            {
                for (int i = 0; i < settings.texts.Length; ++i)
                    if (!settings.texts[i].Visible) settings.texts[i].Text = get_field_value(i).ToString();
            }

            // field
            {
                int x0 = (monitor.width() % rectangle.width) / 2, y0 = (monitor.height() % rectangle.height) / 2;
                int divx = (monitor.width() - x0) / rectangle.width - 1, divy = (monitor.height() - y0) / rectangle.height - 1;
                int x1 = x0 + rectangle.width * divx, y1 = y0 + rectangle.height * divy;
                field.left = x0; field.top = y0; field.right = x1; field.bottom = y1;

                field.graphics = CreateGraphics();
            }

            // label[i]
            {
                int length = field.players > 1 ? 2 : field.players;
                label = new Label[length];
                for (int i = 0; i < length; ++i)
                {
                    int x = i;
                    label[i] = new Label()
                    {
                        AutoSize = true,
                        Font = new Font("Segoe UI", 18F),
                        Location = new Point(((1 + 2 * i) * field.right) / (2 * length), 0),
                        Text = "0"
                    };
                    Controls.Add(label[i]);
                    if (!field.param[2]) label[i].Hide();
                }
            }

            // random
            {
                random.rand = new Random();
            }

            //field
            {
                field.free = new int[field.points()]; field.objects = new obj[field.points()];
                for (int i = 0; i < field.objects.Length; i++) field.objects[i] = new obj(i);

                field.bonus_snakes = field.param[0] ? 1 : 0;
                field.snakes = new snake[field.players + field.bonus_snakes];

                for (int i = 0; i < label.Length; ++i) label[i].ForeColor = field.colors[i];

                field.keys = new Keys[2][]
                {
                    new Keys[4] { Keys.A, Keys.S, Keys.D, Keys.W },
                    new Keys[4] { Keys.Left, Keys.Down, Keys.Right, Keys.Up }
                };
                if (field.param[1]) swap(ref field.keys[0], ref field.keys[1]);
            }

            // creating
            {
                food.count = food.count_su = food.count_sd = 0;
                armor.count = 0;
                armor.image = new Bitmap(Properties.Resources.shield, rectangle.width, rectangle.height);
                field.timer = new Timer();
                time(ref field.timer, 1500, field.creating);
            }

            // snakes
            {
                Timer timer = new Timer();
                time(ref timer, 1, new EventHandler(Event));
                void Event(object s, EventArgs e)
                {
                    if (form() != null)
                    {
                        for (int i = 0; i < field.snakes.Length; ++i)
                            if (i < field.players) field.snakes[i] = new snake(i, field.keys[i % 2], field.colors[i % 2]);
                            else field.snakes[i] = new snake(i);
                        timer.Dispose();
                    }
                }
            }

            // armor
            {
                armor.timers = new Timer[field.players];
                for (int i = 0; i < field.players; ++i) armor.timers[i] = new Timer();
            }

        }
        void Clear()
        {
            for (int i = 0; i < label.Length; ++i) Controls.Remove(label[i]);
            for (int i = 0; i < field.objects.Length; ++i) field.objects[i].Clear();
            for (int i = 0; i < field.snakes.Length; ++i) field.snakes[i].Clear();
            for (int i = 0; i < armor.timers.Length; ++i) armor.timers[i].Dispose();
            food.Clear_bonus(); field.timer.Dispose();
        }
        public static Label[] label;
        public static class main
        {
            public static int length;
            public static Button[] buttons;
            public static void create(string[] names)
            {
                length = names.Length;
                buttons = new Button[length];
                for (int i = 0; i < length; ++i)
                {
                    int x = i;
                    buttons[i] = new Button
                    {
                        Size = new Size(200, 100),
                        Font = new Font("Segoe UI", 18F),
                        Location = new Point(monitor.width() / 2 - 100, ((2 + 2 * i) * monitor.height()) / (2 * length + 4)),
                        Text = names[i]
                    };
                    buttons[i].KeyDown += new KeyEventHandler(delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.ControlKey) form().CursorChange(); }); //error = false; Application.Exit();
                    buttons[i].KeyDown += new KeyEventHandler(delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) hide(x); });
                    buttons[i].MouseClick += new MouseEventHandler(delegate (object s, MouseEventArgs e) { hide(x); });
                }
            }
            public static void show() { for (int i = 0; i < length; ++i) buttons[i].Show(); }
            public static void hide(int id)
            {
                if (id != 2) for (int i = 0; i < length; ++i) buttons[i].Hide();
                switch (id)
                {
                    case 0: form().Clear(); form().Start(); form().Focus(); break;
                    case 1: settings.show("CheckBox"); break;
                    case 2:
                        {
                            string s = "";
                            s += " WASD - первый игрок,  ↑ ← ↓ → - второй игрок\n";
                            s += " Escape/Enter - кнопка ввода, во время игры переводит в меню\n";
                            s += " F5 - перезапуск данной игры\n";
                            MessageBox.Show(s);
                        }
                        break;
                    case 3: { error = false; Application.Exit(); } break;
                    default: break;
                }
            }

        }
        public static class settings
        {
            public static int length;
            public static CheckBox[] check;
            public static Label[] labels;
            public static TextBox[] texts;
            public static PictureBox circle_colors; public static Label circle_label, creating_label;
            public static void create(bool[] param, string[] names)
            {
                length = param.Length;
                check = new CheckBox[length];
                for (int i = 0; i < param.Length; ++i)
                {
                    int x = i;
                    check[i] = new CheckBox
                    {
                        AutoSize = true,
                        Font = new Font("Segoe UI", 10F),
                        Location = new Point(monitor.width() / 8, ((3 + 2 * i) * monitor.height()) / (2 * length + 4)),
                        Checked = param[i],
                        Text = names[i]
                    };
                    check[x].KeyDown += new KeyEventHandler(delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.ControlKey) form().CursorChange(); });

                    if (x < param.Length - 2) check[x].CheckedChanged += new EventHandler(
                            delegate (object s, EventArgs e) { try { form().Clear(); field.param[x] = check[x].Checked; form().Start(); } catch { } Cursor.Show(); cursor = 1; }
                        );
                    else if (x == param.Length - 2)
                    {
                        check[x].CheckedChanged += new EventHandler(
                            delegate (object s, EventArgs e)
                            {
                                field.param[x] = check[x].Checked;
                                if (check[x].Checked) show("texts");
                                else hide("texts");
                            }
                        );
                        check[x].VisibleChanged += new EventHandler(
                            delegate (object s, EventArgs e)
                            {
                                if (check[x].Checked && check[x].Visible) show("texts");
                            }
                        );
                    }
                    else check[x].CheckedChanged += new EventHandler(
                        delegate (object s, EventArgs e)
                        {
                            if (check[x].Checked) { hide("CheckBox"); hide("texts"); main.show(); check[x].Checked = false; }
                        }
                    );
                }
            }
            public static void hide(string type)
            {
                switch (type)
                {
                    case "CheckBox": foreach (CheckBox item in check) { item.Hide(); } break;
                    case "TextBox": foreach (TextBox item in texts) { item.Hide(); } break;
                    case "Label": foreach (Label item in labels) { item.Hide(); } break;
                    case "Circle": circle_colors.Hide(); circle_label.Hide(); creating_label.Hide(); break;
                    case "texts": hide("TextBox"); hide("Label"); hide("Circle"); break;
                    default: break;
                }
            }
            public static void show(string type)
            {
                switch (type)
                {
                    case "CheckBox": foreach (CheckBox item in check) { item.Show(); } break;
                    case "TextBox": foreach (TextBox item in texts) { item.Show(); } break;
                    case "Label": foreach (Label item in labels) { item.Show(); } break;
                    case "Circle": circle_colors.Show(); circle_label.Show(); creating_label.Show(); break;
                    case "texts": show("TextBox"); show("Label"); show("Circle"); break;
                    default: break;
                }
            }
        }
        public void settingsAddTexts(string[] names)
        {
            int length = names.Length;
            settings.labels = new Label[length];
            settings.texts = new TextBox[length];
            for (int i = 0; i < length; ++i)
            {
                int id = i;
                settings.labels[i] = new Label
                {
                    Size = new Size(200, 60),
                    Font = new Font("Segoe UI", 12F),
                    Location = new Point(monitor.width() / 2 - 150, ((2 + 2 * i) * monitor.height()) / (2 * length + 4)),
                    Text = names[i],
                    TextAlign = ContentAlignment.MiddleRight
                };
                settings.labels[i].KeyDown += new KeyEventHandler(delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.ControlKey) CursorChange(); }); //
                Controls.Add(settings.labels[i]);
                settings.labels[i].Hide();

                settings.texts[i] = new TextBox
                {
                    Size = new Size(100, 60),
                    Text = elementary_field_value(i),
                    Font = new Font("Segoe UI", 12F),
                    Location = new Point(monitor.width() / 2 + 60, ((2 + 2 * i) * monitor.height()) / (2 * length + 4) + 15),
                };
                settings.texts[i].KeyDown += new KeyEventHandler(delegate (object s, KeyEventArgs e) { if (e.KeyCode == Keys.ControlKey) CursorChange(); }); //
                settings.texts[i].TextChanged += new EventHandler(delegate (object s, EventArgs e)
                {
                    if (settings.texts[id].Visible)
                    {
                        try
                        {
                            Clear();
                            int val = Convert.ToInt32(settings.texts[id].Text);
                            if ((id != 0 || val > 15) && (id != 5 || val > 0) && val >= 0) change_field_value(id, val);
                            Start();
                            Cursor.Show(); cursor = 1;
                        }
                        catch { }
                    }
                }
                );
                Controls.Add(settings.texts[i]);
            }
        }
        public void settingsAddCircleColors()
        {
            int width = Properties.Resources.circle_design.Width / 2, height = Properties.Resources.circle_design.Height / 2;
            int x = (4 * monitor.width()) / 5 - width / 2, y = monitor.height() / 2 - height / 2;
            int add = 40;

            Bitmap bmp = new Bitmap(width, height + add);
            Color[] colors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Black };
            int len = colors.Length, left = (width - add * len) / 2;
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(new Bitmap(Properties.Resources.circle_design, width, height), 0, 0);
            for (int i = 0; i < len; ++i)
                g.FillRectangle(new SolidBrush(colors[i]), left + i * add, height, add, add);

            settings.circle_colors = new PictureBox()
            {
                Size = new Size(width, height + add),
                Location = new Point(x, y),
                Image = bmp
            };

            settings.circle_colors.MouseClick += MouseEvent;
            Controls.Add(settings.circle_colors);

            void MouseEvent(object s, MouseEventArgs e)
            {
                string str = settings.circle_label.Text;
                int id = str[str.Length - 1] - 49;
                Color color = ((Bitmap)settings.circle_colors.Image).GetPixel(e.X, e.Y);
                field.colors[id] = color;
                Clear(); Start(); CursorChange();
            }

            settings.circle_label = new Label()
            {
                Size = new Size(200, 60),
                Font = new Font("Segoe UI", 12F),
                Location = new Point(x, y / 2),
                Text = "Номер игрока: 1",
                TextAlign = ContentAlignment.MiddleRight
            };
            settings.circle_label.MouseClick += MouseEvent1;
            Controls.Add(settings.circle_label);

            void MouseEvent1(object s, MouseEventArgs e)
            {
                string str = settings.circle_label.Text, ans = "";
                int id = str[str.Length - 1] - 48;
                for (int i = 0; i < str.Length - 1; ++i) ans += str[i];
                ans += (id % 2 + 1).ToString()[0];
                settings.circle_label.Text = ans;
            }

            settings.creating_label = new Label()
            {
                Size = new Size(250, 60),
                Font = new Font("Segoe UI", 12F),
                Location = new Point(x, y + height + add + 20),
                Text = "Режим разброса ресурсов: "+field.creating_mode.ToString(),
                TextAlign = ContentAlignment.MiddleRight
            };
            settings.creating_label.MouseClick += MouseEvent2;
            Controls.Add(settings.creating_label);

            void MouseEvent2(object s, MouseEventArgs e)
            {
                string str = settings.creating_label.Text, ans = "";
                int id = str[str.Length - 1] - 48;
                for (int i = 0; i < str.Length - 1; ++i) ans += str[i];
                ans += (id % 2 + 1).ToString()[0];
                settings.creating_label.Text = ans;
                field.creating_mode = id % 2 + 1;
            }
        }
        int get_field_value(int id)
        {
            int x = -1;
            switch (id)
            {
                case 0: x = rectangle.height; break;
                case 1: x = field.players; break;
                case 2: x = field.max_food_count; break;
                case 3: x = field.max_arm_count; break;
                case 4: x = field.max_speed_count; break;
                case 5: x = snake.v0; break;
                case 6: x = field.mushroom; break;
                case 7: x = snake.finish; break;
                case 8: x = snake.acceleration; break;
            }
            return x;
        }
        void change_field_value(int id, int val)
        {
            switch (id)
            {
                case 0: rectangle.height = rectangle.width = val; break;
                case 1: field.players = val; break;
                case 2: field.max_food_count = val; break;
                case 3: field.max_arm_count = val; break;
                case 4: field.max_speed_count = val; break;
                case 5: snake.v0 = val; break;
                case 6: field.mushroom = val; break;
                case 7: snake.finish = val; break;
                case 8: snake.acceleration = val; break;
            }
        }
        string elementary_field_value(int id)
        {
            string[] values = new string[]
            {
                "35", "1", "5",
                "1", "6",
                "4", "20", $"{int.MaxValue}", "1"
            };
            return values[id];
        }
        public static int cursor = 1;
        void CursorChange()
        {
            if (cursor == 1) { Cursor.Hide(); cursor = 0; }
            else { Cursor.Show(); cursor = 1; }
        }
    }

    class rectangle
    {
        public static int width, height;
    }

    class field
    {
        // var
        public static int players, max_food_count, max_arm_count, max_speed_count, creating_mode;
        public static int mushroom;
        public static int left, right, top, bottom;
        public static int[] free;
        public static obj[] objects;
        public static snake[] snakes;
        public static int bonus_snakes;
        public static Keys[][] keys;
        public static Color[] colors;
        public static Graphics graphics;
        public static Timer timer;
        /// <summary>
        /// mushroom, swap keys, show label
        /// </summary>
        public static bool[] param;

        // parameters
        public static int width() { return (right - left) / rectangle.width + 1; }
        public static int height() { return (bottom - top) / rectangle.height + 1; }
        public static int points() { return height() * width(); }
        public static int move(int coord, int direction)
        {
            int x, y, dx = rectangle.width, dy = rectangle.height;
            coordinate.unzip(coord, out x, out y);
            switch (direction) { case 0: x -= dx; break; case 1: y += dy; break; case 2: x += dx; break; case 3: y -= dy; break; }
            if (x < left) x = right; if (x > right) x = left; if (y > bottom) y = top; if (y < top) y = bottom;
            coordinate.zip(x, y, out coord);
            return coord;
        }
        public static class coordinate
        {
            public static void zip(int x, int y, out int a)
            {
                y -= top; y /= rectangle.height; x -= left; x /= rectangle.width;
                a = y * width() + x;
            }
            public static void unzip(int a, out int x, out int y)
            {
                y = a / width(); x = a % width();
                y = top + y * rectangle.height; x = left + x * rectangle.width;
            }
        }
        public static void fill_rect(int a, Color color)
        {
            int x, y;
            coordinate.unzip(a, out x, out y);

            graphics.FillRectangle(new SolidBrush(color), x, y, rectangle.width, rectangle.height);
        }
        public static void fill_ell(int a, Color color, int dx = 0, int dy = 0)
        {
            int x, y;
            coordinate.unzip(a, out x, out y);

            graphics.FillEllipse(new SolidBrush(color), x + dx, y + dy, rectangle.width - 2 * dx, rectangle.height - 2 * dy);
        }
        public static void fill_arm(int a)
        {
            int x, y;
            coordinate.unzip(a, out x, out y);

            graphics.DrawImage(armor.image, x, y);
        }
        public static void creating(object s, EventArgs e)
        {
            int pos = random.mass(free);
            if (pos != -1)
            {
                int[] p;
                if (creating_mode == 1) p = new int[] { 10, 3, 3, 1 };
                else p = new int[]
                {
                    max_food_count- food.count,
                    max_speed_count-(food.count_su + food.count_sd),
                    max_speed_count-(food.count_su + food.count_sd),
                    max_arm_count - armor.count
                };
                switch (random.mass(p))
                {
                    case 0: if (food.count < max_food_count) objects[pos] = new food(pos); break;
                    case 1: if (food.count_su + food.count_sd < max_speed_count) objects[pos] = new food(pos, "speed up"); break;
                    case 2: if (food.count_sd + food.count_su < max_speed_count) objects[pos] = new food(pos, "speed down"); break;
                    case 3: if (armor.count < max_arm_count) objects[pos] = new armor(pos); break;
                    default: break;
                }
            }
        }
    }

    class snake
    {
        public static int v0, finish, acceleration;
        public int points = 0, t0, speed = 0;
        static int max_bonus;
        public int id;
        List<int> body;
        Timer timer;
        public Color color;
        int dir;
        KeyEventHandler keyEventHandler;
        public snake(int _id, Keys[] _keys, Color _color)
        {
            id = _id;
            t0 = Max(1000 / v0, 1);
            points = 0;
            color = _color;
            dir = random.next(4);
            timer = new Timer();
            body = new List<int>();
            keyEventHandler = new KeyEventHandler(keyEvent);

            body.Add(random.mass(field.free));
            field.fill_rect(body[0], color);
            field.free[body[0]] = 0;
            Game.time(ref timer, t0, new EventHandler(Event));
            Game.form().KeyDown += keyEventHandler;

            void keyEvent(object s, KeyEventArgs e)
            {
                if (e.KeyCode == _keys[0]) upd_dir(0);
                else if (e.KeyCode == _keys[1]) upd_dir(1);
                else if (e.KeyCode == _keys[2]) upd_dir(2);
                else if (e.KeyCode == _keys[3]) upd_dir(3);
            }
            void Event(object s, EventArgs e)
            {
                move(dir);
                if (body.Count > 1)
                {
                    field.fill_rect(body[body.Count - 2], color);
                    if (armor.timers[id].Enabled) field.fill_arm(body[body.Count - 2]);
                }
                field.fill_rect(body[body.Count - 1], color);
                field.fill_ell(body[body.Count - 1], Color.Black);
                field.free[body[body.Count - 1]] = 0;

                int rate = Max(body.Count + 2 * speed, 1);
                if (acceleration == 0) { }
                else if (acceleration == 1) timer.Interval = Max((int)(t0 / Sqrt(rate)), 1);
                else if (acceleration == 2) timer.Interval = Max(t0 / rate, 1);
                else timer.Interval = Max(t0 / (rate * rate), 1);
                int[] sum = new int[2] { 0, 0 };
                for (int i = 0; i < field.players; ++i) sum[i % 2] += field.snakes[i].points + field.snakes[i].body.Count - 1;
                Game.label[id % 2].Text = sum[id % 2].ToString();
                if (sum[id % 2] >= finish)
                {
                    for (int i = 0; i < field.snakes.Length; ++i) field.snakes[i].Clear();
                    for (int i = 0; i < armor.timers.Length; ++i) armor.timers[i].Dispose();
                    food.Clear_bonus(); field.timer.Dispose();
                    if (Game.cursor == 0)
                    {
                        string mes = " Игра окончена! ";
                        string[] str = new string[] { "первый", "второй" };
                        if (field.players > 1) mes += $"\n Победил {str[id % 2]} игрок. ";
                        MessageBox.Show(mes);
                    }
                }
            }
        }
        public snake(int _id)
        {
            id = _id;
            points = 1; speed = 0;
            body = new List<int>();
            timer = new Timer();

            body.Add(random.mass(field.free));
            field.objects[body[0]] = new food(body[0], "bonus");
            Game.time(ref timer, 1000, new EventHandler(Event));

            max_bonus = field.mushroom;
            void Event(object s, EventArgs e)
            {
                if (body.Count == 0)
                {
                    int x = random.mass(field.free);
                    if (x > -1)
                    {
                        body.Add(x);
                        field.objects[x] = new food(x, "bonus");
                        max_bonus = field.mushroom;
                        speed = 0;
                        points = 1;
                    }
                }
                else
                {
                    int rate = Max(body.Count + speed, 1);
                    timer.Interval = (int)Max(1000 / Sqrt(rate), 1);
                    int sz = body.Count;
                    for (int j = 0; j < sz; ++j)
                    {
                        for (int i = 0; i < 4; ++i)
                        {
                            int x = field.move(body[j], i);
                            if (field.objects[x].type != "none" && field.objects[x].type != "bonus")
                            {
                                body.Add(x);
                                switch (field.objects[x].type)
                                {
                                    case "speed up": speed += 10; break;
                                    case "speed down": speed -= 20; break;
                                    default: break;
                                }
                                field.objects[x].Clear();
                                field.objects[x] = new food(x, "bonus");
                                ++max_bonus;
                            }
                        }
                    }
                    sz = body.Count;
                    int[] mass = new int[sz];
                    for (int j = 0; j < sz; ++j)
                    {
                        int sum = 0;
                        for (int i = 0; i < 4; ++i)
                        {
                            int x = field.move(body[j], i);
                            if (field.objects[x].type != "none" && field.objects[x].type != "bonus")
                            {
                                body.Add(x);
                                field.objects[x].Clear();
                                field.objects[x] = new food(x, "bonus");
                            }
                            sum += field.free[x];
                        }
                        mass[j] = sum;
                    }
                    int from = random.mass(mass);
                    if (from > -1)
                    {
                        if (points < max_bonus)
                        {
                            int[] to = new int[4]; mass = new int[4];
                            for (int i = 0; i < 4; ++i)
                            {
                                to[i] = field.move(body[from], i);
                                mass[i] = field.free[to[i]];
                            }
                            int res = random.mass(mass);
                            body.Add(to[res]);
                            field.objects[to[res]] = new food(to[res], "bonus");
                            ++points;
                        }
                        else
                        {
                            from = random.next(sz);
                            field.objects[body[from]].Clear();
                            body.RemoveAt(from);
                            --points;
                        }
                    }
                }
            }
        }
        void move(int dir)
        {
            bool suicide = false, remove = true, let = false;
            int first = body[0], to = field.move(body[body.Count - 1], dir);
            field.free[to] = 0;
            body.RemoveAt(0);

            for (int i = 0; i < field.players; ++i)
            {
                int x = field.snakes[i].body.IndexOf(to);
                if (x > -1)
                {
                    if (!armor.timers[i].Enabled && x + 1 != field.snakes[i].body.Count)
                    {
                        for (int j = 0; j <= x; ++j) field.objects[field.snakes[i].body[j]] = new food(field.snakes[i].body[j], "dead");
                        field.snakes[i].body.RemoveRange(0, x + 1);
                        if (i == id) suicide = true;
                    }
                    else let = true;
                }
            }
            for (int i = field.players; i < field.players + field.bonus_snakes; ++i)
            {
                int x = field.snakes[i].body.IndexOf(to);
                if (x > -1) field.snakes[i].body.RemoveAt(x);
            }

            if (let) body.Insert(0, first);
            else
            {
                bool res = true;
                switch (field.objects[to].type)
                {
                    case "food": if (!suicide) body.Insert(0, first); remove = false; ++points; break;
                    case "bonus": if (!suicide) body.Insert(0, first); remove = false; break;
                    case "dead": if (!suicide) body.Insert(0, first); remove = false; break;
                    case "speed up": ++speed; break;
                    case "speed down": --speed; break;
                    case "armor": armor.to_player(id); break;
                    default: res = false; break;
                }
                if (res) field.objects[to].Clear();

                if (suicide || remove) { field.objects[first].Clear(); field.objects[first] = new obj(first); }

                body.Add(to);
            }
        }
        void upd_dir(int x)
        {
            if (!problem()) dir = x;

            bool problem()
            {
                int to = field.move(body[body.Count - 1], x);
                return body.Count > 1 && to == body[body.Count - 2];
            }
        }
        public void Draw()
        {
            for (int i = 0; i < body.Count; ++i) field.fill_rect(body[i], color);
            field.fill_ell(body[body.Count - 1], Color.Black);
        }
        public void DrawArm()
        {
            for (int i = 0; i < body.Count - 1; ++i) field.fill_arm(body[i]);
        }
        public void Clear()
        {
            if (keyEventHandler != null) Game.form().KeyDown -= keyEventHandler;
            timer.Dispose();
        }
    }

    class obj
    {
        protected int coord;
        public string type;
        protected Timer timer;
        public obj() { }
        public obj(int x)
        {
            coord = x;
            field.fill_rect(coord, Control.DefaultBackColor);
            type = "none";
            field.free[coord] = 1;
        }
        public void Clear()
        {
            field.fill_rect(coord, Control.DefaultBackColor);
            switch (type)
            {
                case "food": --food.count; break;
                case "speed up": --food.count_su; break;
                case "speed down": --food.count_sd; break;
                case "armor": --armor.count; break;
                default: break;
            }
            if (timer != null) timer.Dispose();
            type = "none";
            field.free[coord] = 1;
        }
    }

    class food : obj
    {
        public static int count, count_su, count_sd;
        public int target;
        public Color color;
        static int bonus_target, use = 0;
        static Color bonus_color;
        static Timer bonus_timer;
        Color color1, color2;
        void constr(int x, string _type, Color _color1, Color _color2, int time, bool clear = false, string figure = "rect")
        {
            coord = x;
            type = _type;
            target = -1;
            color1 = _color1; color2 = _color2;
            color = color1;
            timer = new Timer();
            field.free[coord] = 0;
            if (_type == "bonus")
            {
                if (use == 0)
                {
                    bonus_timer = new Timer();
                    bonus_color = color;
                    bonus_target = target;
                    ++use;
                    Game.time(ref bonus_timer, time, new EventHandler(Event));
                }
                Game.time(ref timer, time, new EventHandler(delegate (object s, EventArgs e) { field.fill_rect(coord, bonus_color); }));
            }
            else Game.time(ref timer, time, new EventHandler(Event));
            void Event(object sender, EventArgs e)
            {
                if (_type != "bonus") Code(ref color, ref target);
                else
                {
                    figure = "none";
                    Code(ref bonus_color, ref bonus_target);
                }
            }
            void Code(ref Color color, ref int target)
            {
                bool Equal(Color color1, Color color2)
                {
                    return color1.ToArgb() == color2.ToArgb();
                }
                void change(ref int Clr, int Clr1, int Clr2, int s, int dist)
                {
                    if (Clr1 < Clr2)
                    {
                        if (s == -1)
                        {
                            Clr += dist; Clr = Min(Clr, Clr2);
                        }
                        else
                        {
                            Clr -= dist; Clr = Max(Clr, Clr1);
                        }
                    }
                    else
                    {
                        if (s == -1)
                        {
                            Clr -= dist; Clr = Max(Clr, Clr2);

                        }
                        else
                        {
                            Clr += dist; Clr = Min(Clr, Clr1);
                        }
                    }
                }
                if (Equal(color, color1)) target = -1;
                if (Equal(color, color2)) target = 1;
                int
                    R = color.R, G = color.G, B = color.B,
                    R1 = color1.R, G1 = color1.G, B1 = color1.B,
                    R2 = color2.R, G2 = color2.G, B2 = color2.B;
                change(ref R, R1, R2, target, 1);
                change(ref G, G1, G2, target, 1);
                change(ref B, B1, B2, target, 1);
                color = Color.FromArgb(255, R, G, B);
                if (figure == "rect") field.fill_rect(coord, color);
                else if (figure == "ell") field.fill_ell(coord, color, 6, 6);
                if (clear && Equal(color, color2)) Clear();
            }
        }
        public food(int x)
        {
            ++count;
            constr(x, "food", Color.Yellow, Color.DarkViolet, 80);
        }
        public food(int x, string _type)
        {
            if (_type == "dead") constr(x, _type, Color.Black, Control.DefaultBackColor, 55, true); //*240
            else if (_type == "speed up") { ++count_su; constr(x, _type, Color.Red, Control.DefaultBackColor, 80, true, "ell"); }
            else if (_type == "speed down") { ++count_sd; constr(x, _type, Color.DarkBlue, Control.DefaultBackColor, 80, true, "ell"); }
            else if (_type == "bonus") { constr(x, _type, Color.Green, Control.DefaultBackColor, 20); }
            else if (_type == "food") { ++count; constr(x, _type, Color.Yellow, Color.DarkViolet, 80); }
        }
        public static void Clear_bonus()
        {
            if (bonus_timer != null) bonus_timer.Dispose();
            use = 0;
        }
    }

    class armor : obj
    {
        public static int count;
        public static Bitmap image;
        public static Timer[] timers;
        public armor(int x)
        {
            ++count;
            coord = x;
            field.fill_arm(coord);
            type = "armor";
            field.free[coord] = 0;
            timer = new Timer();
            Game.time(ref timer, 10, delegate (object s, EventArgs e) { field.fill_arm(coord); });
        }
        public static void to_player(int id)
        {
            timers[id].Dispose();
            field.snakes[id].DrawArm();
            Game.time
            (
                ref timers[id],
                13200,
                new EventHandler(delegate (object s, EventArgs e) { timers[id].Dispose(); field.snakes[id].Draw(); })
            );
        }
    }

    class random
    {
        public static Random rand;
        public static int next(int x)
        {
            return rand.Next(x);
        }
        public static int mass(int[] mass)
        {
            int[] sum = new int[mass.Length + 1];
            sum[0] = 0;
            for (int i = 0; i < mass.Length; ++i) sum[i + 1] = sum[i] + mass[i];
            int x = next(sum[mass.Length]);
            for (int i = 0; i < mass.Length; ++i) if (sum[i] <= x && x < sum[i + 1]) return i;
            return -1;
        }
    }

    class monitor
    {
        public static int left() { return Screen.PrimaryScreen.Bounds.Left; }
        public static int width() { return Screen.PrimaryScreen.Bounds.Width; }
        public static int top() { return Screen.PrimaryScreen.Bounds.Top; }
        public static int height() { return Screen.PrimaryScreen.Bounds.Height; }
    }

}