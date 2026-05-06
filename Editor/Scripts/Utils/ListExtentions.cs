using System.Collections.Generic;
using UnityEngine;

namespace FoxEdit
{
    internal static class ListExtentions
    {
        public static T Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            if (newIndex > oldIndex)
                newIndex--;
            T element = list[oldIndex];
            list.Remove(element);
            list.Insert(newIndex, element);

            return element;
        }
    }
}