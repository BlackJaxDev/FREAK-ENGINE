using Extensions;
using System.Numerics;
using XREngine.Core.Attributes;

namespace XREngine.Rendering.UI
{
    /// <summary>
    /// Handles textual input from the user.
    /// </summary>
    [RequireComponents(typeof(UITextComponent))]
    public class UITextInputComponent : UIInteractableComponent
    {
        public UITextComponent TextComponent => GetSiblingComponent<UITextComponent>(true)!;

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }
        private string _preText = string.Empty;
        public string PreText
        {
            get => _preText;
            set => SetField(ref _preText, value);
        }
        private string _postText = string.Empty;
        public string PostText
        {
            get => _postText;
            set => SetField(ref _postText, value);
        }

        /// <summary>
        /// Called on immediately inputted text before it is added to the text.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual bool PreValidateInput(string input) => true;
        /// <summary>
        /// Called on the text after any input text has been added to it.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual bool PostValidateInput(string input) => true;

        public void AddText(string text)
        {
            if (!PreValidateInput(text))
                return;
            string newText = Text.Insert(CursorPosition, text);
            if (PostValidateInput(newText))
            {
                Text = newText;
                CursorPosition += text.Length;
            }
        }
        public void RemoveText(int count)
        {
            if (CursorPosition - count < 0)
                count = CursorPosition;
            string newText = Text.Remove(CursorPosition - count, count);
            if (PostValidateInput(newText))
            {
                Text = newText;
                CursorPosition -= count;
            }
        }

        private int _cursorPosition = 0;
        public int CursorPosition
        {
            get => _cursorPosition;
            set => SetField(ref _cursorPosition, value.Clamp(0, Text.Length));
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Text):
                    //Re-validate cursor position
                    CursorPosition = _cursorPosition;
                    //Display updated text to the user
                    TextComponent.Text = $"{PreText}{Text}{PostText}";
                    break;
            }
        }

        public void SetCursorPositionWithLocalPoint(Vector3 localPoint)
        {
            var text = TextComponent.Text;
            if (string.IsNullOrEmpty(text))
            {
                CursorPosition = 0;
                return;
            }
            var font = TextComponent.Font;
            if (font is null)
            {
                CursorPosition = 0;
                return;
            }

            //TODO

            //var glyphs = TextComponent.Glyphs;
            //if (glyphs is null)
            //{
            //    CursorPosition = 0;
            //    return;
            //}
            //var cursorX = localPoint.X;
            //var cursorY = localPoint.Y;
            //var cursorIndex = 0;
            //var cursorDistance = float.MaxValue;
            //for (int i = 0; i < glyphs.Count; i++)
            //{
            //    var glyph = glyphs[i];
            //    var glyphX = glyph.transform.X;
            //    var glyphY = glyph.transform.Y;
            //    var glyphWidth = glyph.transform.Z;
            //    var glyphHeight = glyph.transform.W;
            //    if (cursorY >= glyphY && cursorY <= glyphY + glyphHeight)
            //    {
            //        var distance = Math.Abs(cursorX - glyphX);
            //        if (distance < cursorDistance)
            //        {
            //            cursorDistance = distance;
            //            cursorIndex = i;
            //        }
            //    }
            //}
            //CursorPosition = cursorIndex;
        }
    }
}
