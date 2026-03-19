using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PGD
{
    public class UIColor : MonoBehaviour
    {
        private Color color_buttonNormal = new Color(1f, 1f, 1f, 1f);
        private Color color_buttonHighlighted = new Color(245f / 255f, 245f / 255f, 245f / 255f, 1f);
        private Color color_buttonPressed = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1f);
        private Color color_buttonDisabled = new Color(1f, 1f, 1f, 1f);

        private Color color_panelAlpha = new Color(1f, 1f, 1f, 60f / 255f);

        protected ColorBlock SetButtonColor()
        {
            ColorBlock cb = gameObject.GetComponent<Button>().colors;

            cb.normalColor = color_buttonNormal;
            cb.highlightedColor = color_buttonHighlighted;
            cb.pressedColor = color_buttonPressed;
            cb.disabledColor = color_buttonDisabled;

            return cb;
        }

        protected Color SetPanelAlphaValue()
        {
            return color_panelAlpha;
        }
    }
}

