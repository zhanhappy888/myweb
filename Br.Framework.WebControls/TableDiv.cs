using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Br.Framework.WebControls
{
    public class TableDiv
    {
        private float left;
        private float top;
        private float width;
        private float height;

        public TableDiv(float left, float top, float width, float height)
        {
            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
        }
        public TableDiv()
        {
            this.left = 0;
            this.top = 0;
            this.width = 0;
            this.height = 0;
        }
        public float Left
        {
            get
            {
                return left;
            }

            set
            {
                left = value;
            }
        }

        public float Top
        {
            get
            {
                return top;
            }

            set
            {
                top = value;
            }
        }

        public float Width
        {
            get
            {
                return width;
            }

            set
            {
                width = value;
            }
        }

        public float Height
        {
            get
            {
                return height;
            }

            set
            {
                height = value;
            }
        }
    }
}
