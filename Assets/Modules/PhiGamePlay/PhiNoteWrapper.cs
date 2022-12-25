using System.Collections;
using System.Collections.Generic;
using Codice.CM.Common;
using Klrohias.NFast.PhiChartLoader.NFast;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteWrapper : MonoBehaviour, IPhiNoteWrapper
    {
        private bool _isRunning = false;
        internal PhiGamePlayer Player;
        private ChartNote _note;
        internal static Quaternion ZeroRotation = Quaternion.Euler(0, 0, 0);
        private ChartLine _line;
        public SpriteRenderer Renderer;
        public bool IsJudged { get; set; } = false;
        public void NoteStart(ChartNote note)
        {
            _isRunning = true;
            this._note = note;
            this._line = Player.Lines[(int) note.LineId];
            this.IsJudged = false;
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
            var newPosY = _note.YPosition - _line.YPosition;
            localPos.y = newPosY;
            transform.localPosition = localPos;
        }
    }
}