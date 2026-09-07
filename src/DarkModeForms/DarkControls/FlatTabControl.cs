using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DarkModeForms
{
	public class FlatTabControl : TabControl
	{
		#region Public Properties

		private Color _lineColor = SystemColors.Highlight;
		[Description("Color for a decorative line"), Category("Appearance")]
		public Color LineColor
		{
			get => _lineColor;
			set { if (_lineColor != value) { _lineColor = value; Invalidate(); } }
		}

		private Color _borderColor = SystemColors.ControlDark;
		[Description("Color for all Borders"), Category("Appearance")]
		public Color BorderColor
		{
			get => _borderColor;
			set { if (_borderColor != value) { _borderColor = value; Invalidate(); } }
		}

		private Color _selectTabColor = SystemColors.ControlLight;
		[Description("Back color for selected Tab"), Category("Appearance")]
		public Color SelectTabColor
		{
			get => _selectTabColor;
			set { if (_selectTabColor != value) { _selectTabColor = value; Invalidate(); } }
		}

		private Color _selectedForeColor = SystemColors.HighlightText;
		[Description("Fore Color for Selected Tab"), Category("Appearance")]
		public Color SelectedForeColor
		{
			get => _selectedForeColor;
			set { if (_selectedForeColor != value) { _selectedForeColor = value; Invalidate(); } }
		}

		private Color _tabColor = SystemColors.ControlLight;
		[Description("Back Color for un-selected tabs"), Category("Appearance")]
		public Color TabColor
		{
			get => _tabColor;
			set { if (_tabColor != value) { _tabColor = value; Invalidate(); } }
		}

		private Color _backColor = SystemColors.Control;
		[Description("Background color for the whole control"), Category("Appearance"), Browsable(true)]
		public override Color BackColor
		{
			get => _backColor;
			set { if (_backColor != value) { _backColor = value; Invalidate(); } }
		}

		private Color _foreColor = SystemColors.ControlText;
		[Description("Fore Color for all Texts"), Category("Appearance")]
		public override Color ForeColor
		{
			get => _foreColor;
			set { if (_foreColor != value) { _foreColor = value; Invalidate(); } }
		}

		private bool _showTabCloseButton = true;
		[Description("Shows a Close Button on each tab"), Category("Appearance")]
		public bool ShowTabCloseButton
		{
			get => _showTabCloseButton;
			set { if (_showTabCloseButton != value) { _showTabCloseButton = value; Invalidate(); } }
		}

		// Runtime-only state updated inside the paint path - deliberately NOT invalidating,
		// otherwise mouse moves over the tab would trigger a repaint storm.
		[Description("Color for the Close Button on each tab"), Category("Appearance")]
		public Color TabCloseColor { get; set; }

		#endregion Public Properties


		public FlatTabControl()
		{
			try
			{
				Appearance = TabAppearance.Buttons;
				DrawMode = TabDrawMode.Normal;
				ItemSize = new Size(0, 0);
				SizeMode = TabSizeMode.Fixed;

				PreRemoveTabPage = null;
				this.DrawMode = TabDrawMode.OwnerDrawFixed;
			}
			catch (Exception ex)
			{
				// Never hide a configuration failure: a half-initialized control is hard to
				// debug. Log it and let the control come up with the base-class defaults.
				System.Diagnostics.Debug.WriteLine("[FlatTabControl] constructor initialization failed: " + ex);
			}
		}

		protected override void InitLayout()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(ControlStyles.UserPaint, true);
			base.InitLayout();

			TabCloseColor = this.ForeColor;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			DrawControl(e.Graphics);
		}


		private delegate bool PreRemoveTab(int indx);
		private PreRemoveTab? PreRemoveTabPage;
		private bool OverCloseTab = false;

		protected override void OnMouseClick(MouseEventArgs e)
		{
			// Reacts to the Click on the Close Tab Button:
			if (ShowTabCloseButton)
			{
				Point p = e.Location;
				for (int i = 0; i < TabCount; i++)
				{
					Rectangle r = GetTabRect(i);
					r.Offset(6, 8);
					r.Width = 12;
					r.Height = 12;
					if (r.Contains(p))
					{
						CloseTab(i);
					}
				}
			}			
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			/* Hightlighs the Close Button when the Mouse is over it  */
			if (ShowTabCloseButton)
			{
				Point p = e.Location;
				for (int i = 0; i < TabCount; i++)
				{
					Rectangle r = GetTabRect(i);
					r.Offset(6, 8);
					r.Width = 12;
					r.Height = 12;

					OverCloseTab = r.Contains(p); //<- Mouse is over the Close button

					if (OverCloseTab)
					{
						// CreateGraphics() returns a Graphics the caller owns - dispose it.
						using (Graphics g = CreateGraphics())
						{
							DrawTab(g, this.TabPages[i], i);
						}
					}
					else
					{
						if (TabCloseColor == Color.Red)
						{
							using (Graphics g = CreateGraphics())
							{
								DrawTab(g, this.TabPages[i], i);
							}
						}
					}
				}
			}
			//base.OnMouseMove(e);
		}
		private void CloseTab(int i)
		{
			if (PreRemoveTabPage != null)
			{
				bool closeIt = PreRemoveTabPage(i);
				if (!closeIt)
					return;
			}
			TabPages.Remove(TabPages[i]);
		}

		internal void DrawControl(Graphics g)
		{
			try
			{
				if (!Visible)
				{
					return;
				}

				Rectangle clientRectangle = ClientRectangle;
				clientRectangle.Inflate(2, 2);

				// Whole Control Background:
				using (Brush bBackColor = new SolidBrush(BackColor))
				{
					g.FillRectangle(bBackColor, ClientRectangle);
				}

				for (int i = 0; i < TabCount; i++)
				{
					DrawTab(g, TabPages[i], i);
					TabPages[i].BackColor = TabColor;
				}

				using (Pen border = new Pen(BorderColor))
				{
					g.DrawRectangle(border, clientRectangle);

					if (SelectedTab != null)
					{
						clientRectangle.Offset(1, 1);
						clientRectangle.Width -= 2;
						clientRectangle.Height -= 2;
						g.DrawRectangle(border, clientRectangle);
						clientRectangle.Width -= 1;
						clientRectangle.Height -= 1;
						g.DrawRectangle(border, clientRectangle);
					}
				}
			}
			catch (Exception ex)
			{
				// Painting is invoked frequently; keep the control alive, but surface the
				// failure in the debugger instead of rendering a silent white control.
				System.Diagnostics.Debug.WriteLine("[FlatTabControl] DrawControl failed: " + ex);
			}
		}

		internal void DrawTab(Graphics g, TabPage customTabPage, int nIndex)
		{
			Rectangle tabRect = GetTabRect(nIndex);
			Rectangle tabTextRect = GetTabRect(nIndex);
			bool isSelected = (SelectedIndex == nIndex);
			Point[] points;

			if (Alignment == TabAlignment.Top)
			{
				points = new[]
				{
					new Point(tabRect.Left+3, tabRect.Bottom),
					new Point(tabRect.Left+3, tabRect.Top + 0),
					new Point(tabRect.Left + 0, tabRect.Top),
					new Point(tabRect.Right - 0, tabRect.Top),
					new Point(tabRect.Right, tabRect.Top + 0),
					new Point(tabRect.Right, tabRect.Bottom),
					new Point(tabRect.Left+3, tabRect.Bottom)
				};
			}
			else
			{
				points = new[]
				{
					new Point(tabRect.Left, tabRect.Top),
					new Point(tabRect.Right, tabRect.Top),
					new Point(tabRect.Right, tabRect.Bottom - 0),
					new Point(tabRect.Right - 0, tabRect.Bottom),
					new Point(tabRect.Left + 0, tabRect.Bottom),
					new Point(tabRect.Left, tabRect.Bottom - 0),
					new Point(tabRect.Left, tabRect.Top)
				};
			}

			// Draws the Tab Header:
			Color HeaderColor = isSelected ? SelectTabColor : BackColor;
			using (Brush brush = new SolidBrush(HeaderColor))
			using (Pen headerPen = new Pen(HeaderColor))
			{
				g.FillPolygon(brush, points);
				g.DrawPolygon(headerPen, points);

				if (isSelected)
				{
					using (Pen linePen = new Pen(BackColor))
					{
						g.DrawLine(linePen,
							new Point(tabRect.Left, tabRect.Top), new Point(tabRect.Left + 3, tabRect.Top));
					}
					using (Pen accentPen = new Pen(Color.DodgerBlue))
					{
						g.DrawLine(accentPen,
							new Point(tabRect.Left + 3, tabRect.Top), new Point(tabRect.Left + tabRect.Width, tabRect.Top));
					}
				}
			}

			// Draws a Close Button:
			if (ShowTabCloseButton)
			{
				Rectangle r = tabTextRect;
				r = GetTabRect(nIndex);
				r.Offset(6, 8); //Vertically Centered
				r.Height = 5;
				r.Width = 5;

				// If Mouse is over the CloseButton, it Draws it in Red, otherwise uses default Color:
				TabCloseColor = OverCloseTab ? Color.Red : this.ForeColor;
				using (Brush b = new SolidBrush(TabCloseColor))
				using (Pen p = new Pen(b))
				{
					// Draws an X:
					g.DrawLine(p, r.X, r.Y, r.X + r.Width, r.Y + r.Height);
					g.DrawLine(p, r.X + r.Width, r.Y, r.X, r.Y + r.Height);
				}
			}			

			// Draws the Title of the Tab:
			Rectangle rectangleF = tabTextRect;
			rectangleF.Y += 2; // Horizontally Centered
			TextRenderer.DrawText(g, customTabPage.Text, Font, rectangleF, isSelected ? SelectedForeColor : ForeColor);
		}
	}
}
