using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteWrapper : MonoBehaviour, IPhiNoteWrapper
    {
        private bool _isRunning = false;
        [HideInInspector] public PhiGamePlayer Player { get; set; }
        internal static Quaternion ZeroRotation = Quaternion.Euler(0, 0, 0);
        public SpriteRenderer Renderer;
        public bool IsJudged { get; set; } = false;
        private static readonly Vector2 NoteDefaultScale = Vector2.one * 2.5f;
        private Transform _transform;
        public PhiNote Note { get; set; }
        // private Vector3 _cachedPosition;
        private void Awake()
        {
            _transform = transform;
        }


        public void NoteStart()
        {
            _isRunning = true;
            this.IsJudged = false;
            Renderer.sprite = Note.Type switch
            {
                NoteType.Tap => Player.TapNoteSprite,
                NoteType.Flick => Player.FlickNoteSprite,
                NoteType.Drag => Player.DragNoteSprite,
                _ => Player.TapNoteSprite
            };
            _transform.localRotation = ZeroRotation;
            _transform.localScale = NoteDefaultScale;
        }

        // void Update()
        // {
        //     if (!_isRunning) return;
        //     if (Player.CurrentBeats >= _note.EndBeats)
        //     {
        //         _isRunning = false;
        //         Player.OnNoteFinalize(this);
        //         return;
        //     }
        //     
        //     // var newPosY = _note.NoteHeight - _unit.YPosition + _yOffset;
        //     // _cachedPosition.y = newPosY;
        //     // _transform.localPosition = _cachedPosition;
        // }
    }
}