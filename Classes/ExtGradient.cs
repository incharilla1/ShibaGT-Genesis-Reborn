using UnityEngine;

namespace ShibaGTGenesisReborn.Classes
{
    public class ExtGradient
    {
        private Gradient _gradient;
        private GradientColorKey[] _colors = new GradientColorKey[]
        {
            new GradientColorKey(new Color(0.06f, 0.06f, 0.06f), 1f),
        };

        public GradientColorKey[] colors
        {
            get => _colors;
            set
            {
                _colors = value;
                if (_gradient != null && _colors != null)
                    _gradient.colorKeys = _colors;
            }
        }

        public Gradient Gradient
        {
            get
            {
                if (_gradient == null)
                {
                    _gradient = new Gradient();
                    if (_colors != null)
                        _gradient.colorKeys = _colors;
                }
                return _gradient;
            }
        }

        public bool isRainbow = false;
        public bool copyRigColors = false;
    }
}
