using Rhythm;

namespace CustomCharacters
{
    public class CustomCharacterInfo
    {
        public string name;
        public string assetBundleName;
        public string animatorControllerName;
        public RhythmPlayerAnimator.ActionStateAnim defaultActionStates;
        public RhythmPlayerAnimator.ActionStateAnim brawlActionStates;
        public RhythmPlayerAnimator.ActionStateAnim runningActionStates;
        public RhythmPlayerAnimator.ActionStateAnim fallingActionStates;
        public float restOffset;
        public float attackOffset;
        public float holdOffset;
    }
}