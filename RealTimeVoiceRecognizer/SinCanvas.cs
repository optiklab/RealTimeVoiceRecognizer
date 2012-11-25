/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RealTimeVoiceRecognizer
{
    ///<summary>
    /// Canvas to draw graphic equalizer.
    ///</summary>
    public class SinCanvas : Canvas
    {
        #region Public methods

        /// <summary>
        /// Adds visual to canvas.
        /// </summary>
        /// <param name="visual">Visual to add.</param>
        public void AddVisual(Visual visual)
        {
            _visuals.Add(visual);
            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }

        /// <summary>
        /// Deletes visual from canvas.
        /// </summary>
        /// <param name="visual">Visual to delete.</param>
        public void DeleteVisual(Visual visual)
        {
            _visuals.Remove(visual);
            base.RemoveVisualChild(visual);
            base.RemoveLogicalChild(visual);
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Gets visual child by its index.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <returns>Visual.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }

        /// <summary>
        /// Count of visual children.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return _visuals.Count;
            }
        }

        #endregion

        #region Private fields

        /// <summary>
        /// Collection of visual.
        /// </summary>
        private List<Visual> _visuals = new List<Visual>();

        #endregion
    }
}
