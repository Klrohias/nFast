using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteWrapper : MonoBehaviour, IPhiNoteWrapper
    {
        private bool _isRunning = false;
        internal PhiGamePlayer Player;
        private PhiNote _note;
        internal static Quaternion ZeroRotation = Quaternion.Euler(0, 0, 0);
        private PhiLine _line;
        private float _yOffset = 0f;
        public SpriteRenderer Renderer;
        public bool IsJudged { get; set; } = false;
        public void NoteStart(PhiNote note)
        {
            _isRunning = true;
            this._note = note;
            this._line = Player.Lines[(int) note.LineId];
            this.IsJudged = false;
            this._yOffset = Player.ScreenAdapter.ToGameYPos(note.YPosition);
            Renderer.sprite = note.Type switch
            {
                NoteType.Tap => Player.TapNoteSprite,
                NoteType.Flick => Player.FlickNoteSprite,
                NoteType.Drag => Player.DragNoteSprite,
                _ => Player.TapNoteSprite
            };
            transform.localRotation = ZeroRotation;
        }

        void Update()
        {
            if (!_isRunning) return;
            if (Player.CurrentBeats >= _note.EndTime + (!IsJudged ? (15f / _line.Speed) : 0f))
            {
                _isRunning = false;
                Player.OnNoteFinalize(this);
                return;
            }

            var localPos = transform.localPosition;
            var newPosY = _note.NoteHeight - _line.YPosition + _yOffset;
            localPos.y = newPosY;
            transform.localPosition = localPos;
        }
    }
}