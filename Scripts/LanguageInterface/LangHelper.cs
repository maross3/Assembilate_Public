using System.Collections;
using System.Collections.Generic;
using InGameTextEditor;
using Language;
using UnityEngine;
using TextEditor = InGameTextEditor.TextEditor;

// todo
// Keyword Support:
// break statements. Syntax err if keyword outside loop

// Syntax Issues:
// ++,-- operators
// 2; // Should be invalid

// bug: 
// string overflow error, breaks browser

// QoL:
// merge console and GUI
// auto complete
// style helper
    // curly braces on line enter
    // when typing right curly brace, remove one \t

// Test runner:
// error message test cases
// syntax test cases

namespace _Dev
{
    public class LangHelper : MonoBehaviour
    {
        [SerializeField] private TextEditor editor;
        private Dictionary<int, string> _templates;
        private string _activeString;
        
        public void OnDropDownChanged(int i) =>
            _activeString = _templates[i];
        
        public void OnGenerateClicked() =>
            InsertText(_activeString);
        
        public void OnRunEditor()
        {
            var src = editor.Text;
            LangName.Run(src);
        }
        
        public void OnClearEditor()
        {
           if (editor.Text == "") return; 
           editor.CaretPosition = new TextPosition(0, 0);
            editor.DeleteText(new Selection(new TextPosition(0,0),
                new TextPosition(editor.Lines.Count - 1, 
                    editor.Lines[^1].Text.Length)));
        }

        public void InsertText(string text)
        {
            OnClearEditor();
            editor.InsertText(new TextPosition(0, 0), text);
        }
        
        private void Start()
        {
            editor.caretColor = Color.white;
            StartCoroutine(LateStart(0.2f));
            _templates = new Dictionary<int, string>
            {
                {0, InitMessage()},
                {1, FibString()},
                {2, CounterString()},
                {3, VarShadowing()}
            };
        }
        IEnumerator LateStart(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            InsertText(InitMessage());
        }
        
        private string InitMessage() =>
            "// Drag me! :)\n" +
            "// Click 'Fib' for an example!\n" +
            "// Click 'Clear' to erase the editor!\n" +
            "// Run to run the code!\n" +
            "// The console has an error and output section\n" +

            "fun Run()\n" +
            "{\n" +
            "\tprint \"Thanks for testing! :D\";\n" +
            "}\n" +
            "Run();" +
            " // Click run :)\n";

        private string CounterString() =>
            "fun MakeCounter()\n" +
            "{\n" +
            "\tvar i = 0;\n" +
            "\tfun Count()\n" +
            "\t{\n" +
            "\t\ti += 1;\n" +
            "\t\tprint i;\n" +
            "\t}\n" +
            "\treturn Count;\n" +
            "}\n\n" +
            "var counter = MakeCounter();\n" +
            "counter(); // \"1\"\n" +
            "counter(); // \"2\"";

        private string FibString() =>
            "fun fib(n)\n" +
            "{\n" +
            "\tif (n <= 1) return n;\n" +
            "\treturn fib(n - 2) + fib(n - 1);\n" +
            "}\n\n" +
            "for (var i = 0; i < 20; i = i + 1)\n" +
            "{\n" +
            "\tprint fib(i);\n" +
            "}";
        
        private string VarShadowing() =>
            "var a = \"global a\";\n" +
            "var b = \"global b\";\n" +
            "{\n" +
            "\tfun showA()\n" +
            "\t{\n" +
            "\t\tprint a; // global a\n" +
            "\t}\n\n" +

            "\tshowA();\n" +
            "\tvar a = \"a in block\";\n" +
            "\tshowA();\n\n" +
            "\tprint a; // a in block\n\n" +
            "\t{\n"  +
            "\t\tfun ShowB()\n" +
            "\t\t{\n" +
            "\t\t\tprint b; // global b\n" +
            "\t\t}\n\n" +

            "\t\tShowB();\n" +
            "\t\tvar b = \"b in block\";\n" +
            "\t\tShowB();\n\n" +
            "\t\tprint b; // b in block\n\n" +
            "\t}\n" +
            "}";
    }
}