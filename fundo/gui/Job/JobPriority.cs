namespace fundo.gui.Job
{
    /// <summary>
    /// Defines the priority levels for scheduled jobs.
    /// Higher priority jobs are executed before lower priority ones.
    /// </summary>
    public enum JobPriority
    {
        /// <summary>
        /// Lowest priority. Job runs when no other jobs are pending.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority. Default for most operations.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority. Job is preferred over normal and low priority jobs.
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority. Job runs as soon as possible.
        /// </summary>
        Critical = 3
    }
}
