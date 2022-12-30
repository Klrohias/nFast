using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiLineWrapper : MonoBehaviour
    {
        public SpriteRenderer LineBody;
        public Transform UpNoteViewport;
        public Transform DownNoteViewport;

        private float _lastAlpha = 1f;

        public void SetAlpha(float val)
        {
            if (_lastAlpha < 0f && val >= 0f)
            {
                var scale = Vector3.one;
                UpNoteViewport.localScale = scale;
                DownNoteViewport.localScale = scale;
            }
            else if (_lastAlpha >= 0f && val < 0f)
            {
                var scale = Vector3.zero;
                UpNoteViewport.localScale = scale;
                DownNoteViewport.localScale = scale;
            }

            var color = LineBody.color;
            color.a = val;
            LineBody.color = color;
            _lastAlpha = val;
        }
    }
}