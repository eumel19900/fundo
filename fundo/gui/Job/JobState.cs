namespace fundo.gui.Job
{
    /// <summary>
    /// Represents the execution state of a job.
    /// </summary>
    public enum JobState
    {
        /// <summary>
        /// Job is waiting to be executed.
        /// </summary>
        Pending,

        /// <summary>
        /// Job is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Job completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Job was cancelled by user request.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Job failed due to an error.
        /// </summary>
        Failed
    }
}
