using System.Collections.Generic;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiNoteActivator : MonoBehaviour
    {
        public PhiGamePlayer Player;
        public ScreenAdapter ScreenAdapter;
        private Queue<PhiNote> _newNotes;
        private List<IPhiUnitWrapper> _unitWrappers;
        private void Start()
        {
            _newNotes = Player.NewNotes;
            _unitWrappers = Player.UnitWrappers;
        }

        private void Update()
        {
            if (!Player.GameRunning) return;

            lock (_newNotes)
            {
                ActivateDisplayNote();
            }
        }

        private void ActivateDisplayNote()
        {
            while (_newNotes.TryDequeue(out var note))
            {

                var noteObj = note.NoteGameObject;
                if (noteObj != null) continue;

                noteObj = note.NoteGameObject =
                    note.Type == NoteType.Hold ? Player.HoldNotePool.RequestObject() 
                        : Player.NotePool.RequestObject();

                var lineWrapper = _unitWrappers[(int) note.unitId];

                var typedWrapper = (PhiLineWrapper) lineWrapper;
                noteObj.transform.parent = note.ReverseDirection
                    ? typedWrapper.DownNoteViewport
                    : typedWrapper.UpNoteViewport;

                var localPos = noteObj.transform.localPosition;
                var noteYOffset = ScreenAdapter.ToGameYPos(note.YPosition);
                localPos.y = note.NoteHeight - Player.Units[(int) note.unitId].YPosition + noteYOffset;
                localPos.x = ScreenAdapter.ToGameXPos(note.XPosition);
                noteObj.transform.localPosition = localPos;

                var noteWrapper = noteObj.GetComponent<IPhiNoteWrapper>();
                noteWrapper.NoteStart(note);
                noteObj.SetActive(true);
            }
        }
    }
}