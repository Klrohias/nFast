using System;
using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiLineWrapper : MonoBehaviour, IPhiUnitWrapper
    {
        public PhiGamePlayer Player;
        public SpriteRenderer LineBody;
        public Transform UpNoteViewport;
        public Transform DownNoteViewport;
        private static readonly Vector2 LineDefaultScale = new Vector2(50f, 0.1f);
        private float _lastAlpha = 1f;
        private Transform _cachedTransform;

        private void Awake()
        {
            _cachedTransform = transform;
        }
        private void Start()
        {
            _cachedTransform.localScale = Player.ScreenAdapter.ScaleVector3(_cachedTransform.localScale);
        }
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

        public void UpdateLineHeight(float height)
        {
            UpNoteViewport.localPosition = Vector3.down * height;
            DownNoteViewport.localPosition = Vector3.up * height;
        }
        public void DoEvent(UnitEventType type, float value)
        {
            switch (type)
            {
                case UnitEventType.Alpha:
                {
                    SetAlpha(value / 255f);
                    break;
                }
                case UnitEventType.MoveX:
                {
                    var pos = transform.localPosition;
                    pos.x = Player.ScreenAdapter.ToGameXPos(value);
                    transform.localPosition = pos;
                    break;
                }
                case UnitEventType.MoveY:
                {
                    var pos = transform.localPosition;
                    pos.y = Player.ScreenAdapter.ToGameYPos(value);
                    transform.localPosition = pos;
                    break;
                }
                case UnitEventType.Rotate:
                {
                    transform.localRotation = Quaternion.Euler(0, 0, -value);
                    Unit.Rotation = -value / 180f * MathF.PI;
                    break;
                }
                case UnitEventType.Speed:
                {
                    break;
                }
                case UnitEventType.ScaleX:
                {
                    var transform = LineBody.transform;
                    var scale = transform.localScale;
                    scale.x = LineDefaultScale.x * value;
                    transform.localScale = scale;
                    break;
                }
                case UnitEventType.ScaleY:
                {
                    var transform = LineBody.transform;
                    var scale = transform.localScale;
                    scale.y = LineDefaultScale.y * value;
                    transform.localScale = scale;
                    break;
                }
                default:
                {
                    $"Unsupported event {type}".Log();
                    break;
                }
            }
        }

        public PhiUnit Unit { get; set; }
    }
}