using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using Verse;

namespace MSE2
{
    [StaticConstructorOnStartup]
    internal static class Assets
    {
        public static readonly Texture2D WidgetMinusSign = ContentFinder<Texture2D>.Get( "UI/Buttons/Minus", true );

        public static readonly Texture2D WidgetPlusSign = ContentFinder<Texture2D>.Get( "UI/Buttons/Plus", true );

        public static readonly Texture2D WidgetComplete = ContentFinder<Texture2D>.Get( "UI/Widgets/Complete", true );

        public static readonly Texture2D WidgetPartial = ContentFinder<Texture2D>.Get( "UI/Widgets/Partial", true );
    }
}