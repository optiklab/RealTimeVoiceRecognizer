/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Interaction logic for GraphicEqualizerControl.xaml
    /// </summary>
    public partial class GraphicEqualizerControl : UserControl
    {
        #region Constructor
        
        public GraphicEqualizerControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        public void UnloadEqualizer()
        {
            if (_equalizer != null)
            {
                _equalizer.Dispose();
                _equalizer = null;
            }
        }

        /// <summary>
        /// Process current sound buffer.
        /// </summary>
        /// <param name="buffer">Sound buffer.</param>
        /// <param name="data">Data.</param>
        public void ProcessSound(List<byte> buffer, IntPtr data)
        {
            if (_equalizer != null)
                _equalizer.ProcessSound(buffer, data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="audioSettings"></param>
        public void UpdateEqualizer(AudioSettings audioSettings)
        {
            if (audioSettings.isEightBitSample)
                _equalizer = new Equalizer8Bit(audioSettings, DrawingSpace1.ActualWidth, DrawingSpace1.ActualHeight, _UpdateGraphics);
            else
                _equalizer = new Equalizer16Bit(audioSettings, DrawingSpace1.ActualWidth, DrawingSpace1.ActualHeight, _UpdateGraphics);
        }

        #endregion

        #region Private event handlers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnloadEqualizer();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="leftChannelRectangles"></param>
        /// <param name="rightChannelRectangles"></param>
        /// <param name="leftChannelPoints"></param>
        /// <param name="rightChannelPoints"></param>
        private void _UpdateGraphics(IList<Rect> leftChannelRectangles,
            IList<Rect> rightChannelRectangles, IList<Point> leftChannelPoints,
            IList<Point> rightChannelPoints)
        {
            if (leftChannelRectangles != null)
                _UpdateBands(leftChannelRectangles);

            if (rightChannelRectangles != null)
                _UpdateBands(rightChannelRectangles);

            if (leftChannelPoints != null)
                _UpdateGraphs(leftChannelPoints);

            if (rightChannelPoints != null)
                _UpdateGraphs(rightChannelPoints);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rectangles"></param>
        private void _UpdateBands(IList<Rect> rectangles)
        {
            Debug.Assert(rectangles != null);

            this.Dispatcher.BeginInvoke((ThreadStart)delegate()
            {
                DrawingSpace1.DeleteVisual(_visual1);
                DrawingVisual visual = new DrawingVisual();

                using (DrawingContext dc = visual.RenderOpen())
                {
                    Pen pen = new Pen(Brushes.Red, 2);

                    foreach (var rect in rectangles)
                    {
                        dc.DrawRectangle(Brushes.AliceBlue, pen, rect);
                    }

                    DrawingSpace1.DeleteVisual(_visual1);
                    _visual1 = visual;
                    DrawingSpace1.AddVisual(visual);
                }
            }, new object[0]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        private void _UpdateGraphs(IList<Point> points)
        {
            Debug.Assert(points != null);

            this.Dispatcher.BeginInvoke((ThreadStart)delegate()
            {
                DrawingSpace2.DeleteVisual(_visual2);
                DrawingVisual visual = new DrawingVisual();

                using (DrawingContext dc = visual.RenderOpen())
                {
                    Pen pen = new Pen(Brushes.Red, 2);

                    for (int i = 1; i < points.Count - 1; i += 2)
                    {
                        dc.DrawLine(pen, points[i - 1], points[i]);
                    }

                    DrawingSpace2.DeleteVisual(_visual2);
                    _visual2 = visual;
                    DrawingSpace2.AddVisual(visual);
                }
            }, new object[0]);
        }

        #endregion

        #region Pivate fields

        /// <summary>
        /// Graphic equalizer.
        /// </summary>
        private EqualizerBase _equalizer;

        /// <summary>
        /// 
        /// </summary>
        private DrawingVisual _visual1 = new DrawingVisual();

        /// <summary>
        /// 
        /// </summary>
        private DrawingVisual _visual2 = new DrawingVisual();

        #endregion
    }
}
