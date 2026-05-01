using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNoom.UnityGameTool.Typewriter
{
    /// <summary>
    /// 打字机效果配置。支持内联序列化，可直接在 Inspector 中编辑。
    /// </summary>
    [Serializable]
    public class TypewriterConfig
    {
        [Header("基础速度")]
        [Tooltip("每秒显示的字符数")]
        [Range(1f, 120f)]
        [SerializeField]
        private float _charactersPerSecond = 30f;

        [Header("标点延迟")]
        [Tooltip("是否启用标点符号延迟")]
        [SerializeField]
        private bool _enablePunctuationDelay = true;

        [Tooltip("标点符号延迟倍率（相对于基础延迟）")]
        [Range(1f, 20f)]
        [SerializeField]
        private float _punctuationDelayMultiplier = 5f;

        [Tooltip("需要额外延迟的标点符号字符集")]
        [SerializeField]
        private string _punctuationCharacters = "。，！？!?,.;：；…—~";

        [Header("特殊延迟")]
        [Tooltip("换行符延迟倍率（相对于基础延迟）")]
        [Range(1f, 20f)]
        [SerializeField]
        private float _newLineDelayMultiplier = 3f;

        [Tooltip("空格是否跳过延迟（即空格瞬间显示）")]
        [SerializeField]
        private bool _skipSpaceDelay = true;

        private HashSet<char> _punctuationSet;
        private string _cachedPunctuationString;

        /// <summary>每秒显示的字符数</summary>
        public float CharactersPerSecond => _charactersPerSecond;

        /// <summary>标点符号字符集合</summary>
        private HashSet<char> PunctuationSet
        {
            get
            {
                if (_punctuationSet == null || _cachedPunctuationString != _punctuationCharacters)
                {
                    _punctuationSet = new HashSet<char>(_punctuationCharacters);
                    _cachedPunctuationString = _punctuationCharacters;
                }

                return _punctuationSet;
            }
        }

        /// <summary>
        /// 获取指定字符的显示延迟时间（秒）。
        /// </summary>
        /// <param name="c">目标字符</param>
        /// <returns>延迟秒数，0 表示无延迟</returns>
        public float GetDelay(char c)
        {
            float baseDelay = 1f / _charactersPerSecond;

            if (c == ' ')
            {
                return _skipSpaceDelay ? 0f : baseDelay;
            }

            if (c == '\n' || c == '\r')
            {
                return baseDelay * _newLineDelayMultiplier;
            }

            if (_enablePunctuationDelay && PunctuationSet.Contains(c))
            {
                return baseDelay * _punctuationDelayMultiplier;
            }

            return baseDelay;
        }
    }
}
