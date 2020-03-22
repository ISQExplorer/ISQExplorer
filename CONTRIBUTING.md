# Contributing
## Table of contents
* [Git](#git)
    * [Tutorial](#tutorial)
        * [git clone](#git-clone)
        * [git init](#git-init)
        * [git checkout](#git-checkout)
        * [git commit](#git-commit)
        * [git push](#git-push)
        * [git branch](#git-branch)
        * [git merge](#git-merge)
    * [Git Guidelines](#git-guidelines)
* [Code Style](#code-style)
    * [Universal](#universal)
    * [C#](#c#)
    * [CSS](#css)
    * [HTML](#html)
    * [React/TSX](#react/tsx)
    * [Typescript](#typescript)

## Git
The project uses `git` for its version control, as it is by far the most popular version control system both in the industry and for open-source projects because it is well-featured, relatively easy to use, and fast.

### Tutorial
The most important concept to take away from this tutorial is that `git` is essentially a tree where the nodes are "snapshots" of your code. These "snapshots" are called "commits," and they represent how your code was at a point in time. This allows you to track the history of your project as well as revert a file or the entire project to a known working state if you happen to break something.

#### git clone
Before making your changes to the project, download a local copy of the project using
```shell
$ git clone https://github.com/jonathan-lemos/ISQExplorer
```
in any terminal with `git` installed. On Windows, you can use [Git Bash](https://gitforwindows.org/) or [Windows Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10) to get a `git` enabled terminal. On most Linux distributions, `git` is preinstalled, and if it's not you usually know how to install it. If you're on OSX then get a real operating system.

Alternatively, your IDE should be able to `git clone` projects for you. This is not an excuse to forego the terminal for all things `git`, as more complicated procedures are more easily done in the terminal.

The above will create an exact copy of the project as it appears on the git server (in this case GitHub).

#### git init
Alternatively, if you are making a fresh git project, you can instead navigate to the project's directory and
```shell
$ git init
```
to initialize a fresh git repository.

Again, your IDE should also be able to do this for you, but it's better to do it from the terminal.

#### git checkout
To view the history of the project (jump around the tree), you can use
```shell
$ git log
```
This will print out a list of commits in descending order of time, along with who made the commit and a short description of what the commit did.

To switch to a commit, you can
```shell
$ git checkout <commit sha1>
```
Because it is a pain in the ass to type a commit's SHA1 hash every time you want to switch to it, you can instead type
```shell
$ git checkout HEAD~[Number]
```
to go `[Number]` commits behind the `HEAD`. The `HEAD` is the commit you are currently viewing. By default this is the latest commit of the `master` [branch]("#git-branch").

You will not be able to make changes to this point of the project; you will only be able to view the project and copy any files from this point. If you want to make changes to a commit in the past, you're over your head and should consult the internet to make sure you don't fuck anything up.

To go back to the latest commit of your branch, type
```shell
$ git checkout [branch]
```
The default `[branch]` is `master`.

#### git commit
Before making a commit, make sure your `HEAD` points to the latest commit of your branch (see [git checkout](#git-checkout)).

Make your changes to the code using your IDE or text editor.

Then, you
```shell
$ git add -A
```
to add all the changes you have made to your commit. If you would only like to add a few files,
```shell
$ git add [file1] [file2] ...
```
to add those specific files to git.

Before you commit, you should
```shell
$ git status
```
to make sure you don't commit any files you don't want to.

If you make a mistake,
```shell
$ git reset HEAD
```
to unstage your changes.

If you want to prevent files from being tracked by `git` (such as passwords and IDE specific configuration), add their paths to a file called `.gitignore` in your project directory.

Once you have made sure you are adding the right files,
```shell
$ git commit
```
to make a commit.

An editor will come up where you should put a descriptive, short message of what the commit changes.

If you make a mistake with a commit *and you have not pushed your changes*, you can make your corrections and then
```shell
$ git commit --amend
```
to fix the latest commit.

#### git push
Until you `push` your changes, `git` only keeps track of them locally. To push you changes to the server, use
```shell
$ git push origin [branch]
```
You will likely be prompted for your GitHub credentials before the changes are pushed.

If you rewrite your project's history (if you have to ask, you're over your head), then use
```shell
$ git push origin [branch] --force
```
to overwrite the history of the project. **Do not do this unless you really know what you're doing, as you can mess up your project permanently.**

#### git branch
When you first clone a project, you will most likely be dropped into a "branch" called master. Branches, like on an actual tree, represent points where the development of the tree splits. The trunk (`master`) goes one way, and the branch goes another way. This way, you and your teammates can work on the same code at the same time without stepping on each others' toes, since each of you have a separate copy of the code.

It's generally good practice to make branches for the development of features while keeping `master` as a good, clean, working version of the code. This means you should only push urgent security fixes or minor changes directly to `master`.

To make a branch, use
```
$ git branch [branch-name]
```
This will make a new branch with its "base" at the current commit. That means you start out with a clone of the project based on the commit you were on, but any changes you make to the new branch will be independent of the branch its based on and vice versa.

Once you have made the branch, use
```
$ git checkout [branch-name]
```
to switch to it. You can also use this to switch back to the branch you came from.

If you wish to delete a branch, use
```
$ git branch -d [branch-name]
```
to delete the branch locally, and
```
$ git push origin --delete [branch-name]
```
to delete it on the server (if you have pushed it previously). *Be warned that this will delete any unmerged code on that branch*.

#### git merge
When you are done developing the feature your branch is supposed to, you will have to `merge` the changes with the branch it came from. Be warned that this is a complicated and error-prone process, and you should triple-check the merged code to make sure it's working before you push your changes.

The following command will merge the current branch with the target branch, *storing the changes in the current branch*. The target branch is not affected.
```shell
$ git merge [target-branch]
```

The hard part is that if the two branches have modified the same file in two different ways, you will have a "merge conflict". For example, if the original code is
```c
int x = 4;
int y = 7;
int z = 10;
```
and `branch1` (the current branch) changes it to
```c
int x = 4;
int y = 1337;
int z = 10;
```
and `branch2` (the target branch) changes it to
```c
int x = 4;
int y = 7;
const char* yy = "seven";
int z = 10;
```
then `git merge` will output a file that looks like
```c
int x = 4;
<<<<<<< HEAD
int y = 1337;
=======
int y = 7;
const char* yy = "seven";
>>>>>>> branch2
int z = 10;
```
**If you do not understand the above example, you should not perform a `git merge`.**

`git merge` will "merge" both changes into the same file, and it is up to you to resolve these conflicts so that the code works. Most IDE's will automatically highlight these "merge conflict" markers so merge conflicts are easily seen. You shouldn't push your changes until all merge conflicts are resolved.

### Git Guidelines
The guidelines given are specific for this project. If you do not understand any of the termiology used, consult the [tutorial](#tutorial).

* **Do not push with `--force` unless absolutely necessary.**
    * If you do not understand what the above means, read the [tutorial](#tutorial).
    * If you have to ask, don't do it.

* **Do not push *directly* to `master` unless absolutely necessary or your change can be done in a single commit.**
    * If you do not understand what the above means, read the [tutorial](#tutorial).
    * `master` should always be a clean, working version of the code.
    * Every branch directly or indirectly depends on `master`, so it is of the utmost importance that code pushed to `master` is of the highest quality.

* Do leave a good commit message. Don't leave some garbage like `aids` or `minor fixes` or `it works`. Something like `fixed bug where text overflows from the table`, or `added a component that displays professor information` is good enough.

* Follow the other style guides given in this project.

## Code Style
Follow the below style guides to ensure that the project's style remains consistent.

### Universal
* Tabs are 4 spaces unless otherwise specified.
* Do not leave trailing whitespace on lines.
* When in doubt, prefer readability over conciseness.

### C#
* Tabs are 4 spaces.
* Do not leave unused imports.
* Async functions should have "Async" as the last part of their name.

### CSS

### HTML

### React/TSX

### Typescript
