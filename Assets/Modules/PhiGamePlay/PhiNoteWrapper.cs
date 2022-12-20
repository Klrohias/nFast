using System.Collections;
using System.Collections.Generic;
using Codice.CM.Common;
using Klrohias.NFast.PhiChartLoader.NFast;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteWrapper : MonoBehaviour
    {
        private bool isRunning = false;
        public PhiGamePlayer Player;
        private ChartNote note;
        private static Quaternion ZeroRotation = Quaternion.Euler(0, 0, 0);
        private float lastBeats = 0f;
        private float lastSpeed = 0f;
        private PhiLineWrapper line;

        public void NoteStart(ChartNote note, PhiLineWrapper line)
        {
            isRunning = true;
            this.note = note;
            this.line = line;
            this.lastBeats = Player.CurrentBeats;
            this.lastSpeed = line.Speed;
            transform.localRotation = ZeroRotation;
        }

        void Update()
        {
            if (!isRunning) return;
            if (Player.CurrentBeats >= note.EndTime.Beats)
            {
                isRunning = false;
                Player.OnNoteFinalize(this);
                return;
            }

            var localPos = transform.localPosition;
            var deltaBeats = Player.CurrentBeats - lastBeats;
            var deltaSpeed = line.Speed - lastSpeed;
            localPos.y -= lastSpeed * deltaBeats + deltaSpeed * deltaBeats / 2f;
            this.lastBeats = Player.CurrentBeats;
            this.lastSpeed = line.Speed;
            transform.localPosition = localPos;
        }
    }
}