using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiHoldNoteWrapper : MonoBehaviour, IPhiNoteWrapper
    {
        public Transform TopHead;
        public Transform BottomHead;
        public Transform Body;
        [HideInInspector] public PhiGamePlayer Player { get; set; }
        private const float BODY_HEIGHT = 0.04f;
        private const float Y_SCALE = 2.5f;
        public PhiNote Note { get; set; }
        public bool IsJudged { get; set; } = false;
        public void NoteStart()
        {
            transform.localRotation = PhiNoteWrapper.ZeroRotation;
            BottomHead.localPosition = Vector3.zero;
            Body.localScale = new Vector3(1f, Note.NoteLength / BODY_HEIGHT / Y_SCALE, 1f);
            TopHead.localPosition = Vector3.up * Note.NoteLength / Y_SCALE;
            Body.localPosition = Vector3.up * (Note.NoteLength / Y_SCALE / 2f);
        }
    }
}
