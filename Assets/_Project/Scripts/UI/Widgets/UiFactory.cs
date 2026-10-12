using System;
using UnityEngine.UIElements;

namespace Fireflies.UI.Widgets
{
    public static class UiFactory
    {
        public const string HiddenClass = "hidden";

        public static void SetDisplayed(VisualElement element, bool isDisplayed)
        {
            element.EnableInClassList(HiddenClass, !isDisplayed);
        }

        public static Label Label(string text, params string[] classes)
        {
            Label label = new Label(text);
            AddClasses(label, classes);
            return label;
        }

        public static VisualElement Element(params string[] classes)
        {
            VisualElement element = new VisualElement();
            AddClasses(element, classes);
            return element;
        }

        // Los botones no toman foco: si lo tuvieran, la barra espaciadora (embestida) los activaría.
        public static Button Button(string text, Action onClick, params string[] classes)
        {
            Button button = new Button(onClick) { text = text, focusable = false };
            AddClasses(button, classes);
            return button;
        }

        public static Button IconButton(string iconClass, string tooltip, Action onClick)
        {
            Button button = Button(string.Empty, onClick, "icon-button", "icon-button--small");
            button.tooltip = tooltip;
            button.Add(Element("icon", "icon--small", iconClass));
            return button;
        }

        public static void MakeButtonsUnfocusable(VisualElement root)
        {
            root.Query<Button>().ForEach(button => button.focusable = false);
        }

        private static void AddClasses(VisualElement element, string[] classes)
        {
            foreach (string className in classes)
            {
                element.AddToClassList(className);
            }
        }
    }
}
