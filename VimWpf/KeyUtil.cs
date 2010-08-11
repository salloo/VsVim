﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Vim.Extensions;

namespace Vim.UI.Wpf
{
    public static class KeyUtil
    {
        private static ReadOnlyCollection<char> s_coreChars = null;
        private static ReadOnlyCollection<Tuple<char, int, KeyModifiers>> s_mappedCoreChars;

        private static ReadOnlyCollection<char> CoreChars
        {
            get
            {
                if (s_coreChars == null)
                {
                    s_coreChars = CreateCoreChars();
                }
                return s_coreChars;
            }
        }

        private static ReadOnlyCollection<Tuple<char, int, KeyModifiers>> MappedCoreChars
        {
            get
            {
                if (s_mappedCoreChars == null)
                {
                    s_mappedCoreChars = CreateMappedCoreChars();
                }
                return s_mappedCoreChars;
            }
        }

        private static Tuple<KeyInput, int> TryConvertToKeyInput(Key key)
        {
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var opt = InputUtil.TryVirtualKeyCodeToKeyInput(virtualKey);
            return opt.IsSome()
                ? Tuple.Create(opt.Value, virtualKey)
                : null;
        }

        public static KeyModifiers ConvertToKeyModifiers(ModifierKeys keys)
        {
            var res = KeyModifiers.None;
            if (0 != (keys & ModifierKeys.Shift))
            {
                res = res | KeyModifiers.Shift;
            }
            if (0 != (keys & ModifierKeys.Alt))
            {
                res = res | KeyModifiers.Alt;
            }
            if (0 != (keys & ModifierKeys.Control))
            {
                res = res | KeyModifiers.Control;
            }
            return res;
        }

        public static ModifierKeys ConvertToModifierKeys(KeyModifiers keys)
        {
            var res = ModifierKeys.None;
            if (0 != (keys & KeyModifiers.Shift))
            {
                res |= ModifierKeys.Shift;
            }
            if (0 != (keys & KeyModifiers.Control))
            {
                res |= ModifierKeys.Control;
            }
            if (0 != (keys & KeyModifiers.Alt))
            {
                res |= ModifierKeys.Alt;
            }
            return res;
        }

        public static KeyInput ConvertToKeyInput(Key key)
        {
            var tuple = TryConvertToKeyInput(key);
            return tuple != null
                ? tuple.Item1
                : InputUtil.CharToKeyInput(Char.MinValue);
        }

        public static KeyInput ConvertToKeyInput(Key key, ModifierKeys modifierKeys)
        {
            var modKeys = ConvertToKeyModifiers(modifierKeys);
            var tuple = TryConvertToKeyInput(key);
            if (tuple == null)
            {
                return new KeyInput(0, VimKey.NotWellKnown, modKeys, Char.MinValue);
            }

            if ((modKeys & KeyModifiers.Shift) == 0)
            {
                var temp = tuple.Item1;
                return new KeyInput(temp.VirtualKeyCode, temp.Key, modKeys, temp.Char);
            }

            // The shift flag is tricky.  There is no good API available to translate a virtualKey 
            // with an additional modifier.  Instead we define the core set of keys we care about,
            // map them to a virtualKey + ModifierKeys tuple.  We then consult this map here to see
            // if we can appropriately "shift" the KeyInput value
            //
            // This feels like a very hackish solution and I'm actively seeking a better, more thorough
            // one

            var ki = tuple.Item1;
            var virtualKey = tuple.Item2;
            var found = MappedCoreChars.FirstOrDefault(x => x.Item2 == virtualKey && KeyModifiers.Shift == x.Item3);
            if (found == null)
            {
                return new KeyInput(ki.VirtualKeyCode, ki.Key, modKeys, ki.Char);
            }
            else
            {
                return new KeyInput(ki.VirtualKeyCode, ki.Key, modKeys, found.Item1);
            }
        }

        private static ReadOnlyCollection<char> CreateCoreChars()
        {
            return InputUtil.CoreCharacters.ToList().AsReadOnly();
        }

        private static ReadOnlyCollection<Tuple<char, int, KeyModifiers>> CreateMappedCoreChars()
        {
            var list = CoreChars
                .Select(x => Tuple.Create(x, InputUtil.TryCharToVirtualKeyAndModifiers(x)))
                .Where(x => x.Item2.IsSome())
                .Select(x => Tuple.Create(x.Item1, x.Item2.Value.Item1, x.Item2.Value.Item2))
                .ToList();
            return new ReadOnlyCollection<Tuple<char, int, KeyModifiers>>(list);
        }

        public static Tuple<Key, ModifierKeys> ConvertToKeyAndModifiers(KeyInput input)
        {
            var mods = ConvertToModifierKeys(input.KeyModifiers);
            var option = InputUtil.TryCharToVirtualKeyAndModifiers(input.Char);
            var key = Key.None;
            if (option.IsSome())
            {
                key = KeyInterop.KeyFromVirtualKey(option.Value.Item1);
            }

            return Tuple.Create(key, mods);
        }

        public static Key ConvertToKey(KeyInput input)
        {
            return ConvertToKeyAndModifiers(input).Item1;
        }
    }
}
