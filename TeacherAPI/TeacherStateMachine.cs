namespace TeacherAPI
{
    public class TeacherState : NpcState
    {
        protected Teacher teacher;
        public TeacherState(Teacher teacher) : base(teacher)
        {
            this.teacher = teacher;
        }

        /// <summary>
        /// Avoid using this to add anger to your Teacher
        /// </summary>
        public virtual void NotebookCollected(int currentNotebooks, int maxNotebooks)
        {

        }

        /// <summary>
        /// Triggered when the player has completed the activity correctly
        /// <para>The teacher whose notebook is in that activity will automatically have their anger raised after their own notebook has been collected, avoid using this for raising anger.</para>
        /// </summary>
        /// <param name="timer">The amount of time given before the teacher returns back to their chase state.</param>
        public virtual void GoodMathMachineAnswer(float timer)
        {

        }

        /// <summary>
        /// Triggered when the player has completed the activity incorrectly
        /// <para>The teacher whose notebook is in that activity will automatically have their anger raised after an incorrect activity that contains their notebook, avoid using this for raising anger.</para>
        /// </summary>
        public virtual void BadMathMachineAnswer()
        {

        }

        /// <summary>
        /// Triggered when the player exits the spawn.
        /// <para>Avoid using this for raising anger</para>
        /// </summary>
        public virtual void PlayerExitedSpawn()
        {

        }
    }
}
