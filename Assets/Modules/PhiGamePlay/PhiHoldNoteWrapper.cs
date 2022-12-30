using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiHoldNoteWrapper : MonoBehaviour, IPhiNoteWrapper
    {
        private bool _isRunning = false;
        public Transform TopHead;
        public Transform BottomHead;
        public Transform Body;
        private PhiNote _note;
        internal PhiGamePlayer Player;
        private PhiLine _line;
        private const float BODY_HEIGHT = 0.04f;
        private const float Y_SCALE = 2.5f;
        private float _noteLast = 0f;
        private float _yOffset = 0f;
        public bool IsJudged { get; set; } = false;
        private void Update()
        {
            if (!_isRunning) return;
            if (Player.CurrentBeats >= _note.EndTime + (!IsJudged ? _noteLast : 0f))
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

        public void NoteStart(PhiNote note)
        {
            _isRunning = true;
            this._note = note;
            this._line = Player.Lines[(int)note.LineId];
            this._noteLast = note.EndTime - note.BeginTime;
            this._yOffset = Player.ScreenAdapter.ToGameYPos(note.YPosition);
            transform.localRotation = PhiNoteWrapper.ZeroRotation;
            BottomHead.localPosition = Vector3.zero;
            Body.localScale = new Vector3(1f, note.NoteLength / BODY_HEIGHT / Y_SCALE, 1f);
            TopHead.localPosition = Vector3.up * note.NoteLength / Y_SCALE;
            Body.localPosition = Vector3.up * (note.NoteLength / Y_SCALE / 2f);
        }

    }
}
