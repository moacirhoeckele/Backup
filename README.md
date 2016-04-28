# Backup
This is a really simple backup program developed using .NET Framework.
## Motivation
I decided to develop this program because I wanted a simple program to backup my files, but I didn't found one.
So, I also decided to use [Pipeline pattern](https://msdn.microsoft.com/en-us/library/ff963548.aspx) to learn more about this.
## How to use it
The backup program waits for an instruction in the following format:
```
Backup [/E | /e] [source] [destination]
```
- **/E** or **/e** - Leaves the console without waiting for any key be pressed.
- **source** - All folders or files that must be copied separated by comma.
- **destination** - Path to where the source should be copied.

Example:
```
C:\>Backup /e C:\Users\user\Documents,C:\Users\user\Pictures F:\Backup
```
I suggest you use the [Windows Task Scheduler](https://msdn.microsoft.com/en-us/library/windows/desktop/aa383614%28v=vs.85%29.aspx) to schedule the backup at time you prefer.