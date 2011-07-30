using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RemoteOverlayServer
{
    public partial class OverlayForm : Form
    {
        public OverlayForm()
        {
            InitializeComponent();
        }

        public Bitmap getImage()
        {
            Bitmap bitmap = new Bitmap(label1.Width, label1.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            label1.DrawToBitmap(bitmap, new Rectangle(0, 0, label1.Width, label1.Height));
            return bitmap;
        }

        public void setText(string text)
        {
            label1.Text = text;
        }

        public float getFontSize()
        {
            return label1.Font.SizeInPoints;
        }

        public void setFontSize(float size)
        {
            FontFamily calibri = new FontFamily("Calibri");
            Font f = new Font(calibri, size, FontStyle.Bold, GraphicsUnit.Point);
            label1.Font = f;
        }

        public void setLocation(Point p)
        {
            this.Location = p;
        }
    }
}
