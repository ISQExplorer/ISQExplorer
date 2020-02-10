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
        * [git rebase](#git-rebase)

## Git
The project uses `git` for its version control, as it is by far the most popular version control system both in the industry and for open-source projects because it is well-featured, relatively easy to use, and fast.

### Tutorial
The most important concept to take away from this tutorial is that `git` is essentially a tree where the nodes are "snapshots" of your code. These "snapshots" are called "commits," and they represent how your code was at a point in time. This allows you to track the history of your project as well as revert a file or the entire project to a known working state if you happen to break something.

#### git clone
Before making your changes to the project, download a local copy of the project using
```shell
$ git clone https://github.com/jonathan-lemos/ISQExplorer
```
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
to go `[Number]` commits behind the `HEAD`.

You will not be able to make changes to this point of the project; you will only be able to view the project and copy any files from this point. If you want to make changes to a commit in the past, you're over your head and should consult the internet to make sure you don't fuck anything up.

#### git commit
Make sure your

#### git branch 
When you first clone a project, you will most likely be dropped into a "branch" called master. Branches, like on an actual tree, represent points where the development of the tree splits. The trunk (`master`) goes one way, and the branch goes another way. This way, you and your teammates can work on the same code at the same time without stepping on each others' toes, since each of you have a separate copy of the code.

To make a branch, 


When making any changes to the project, make a new branch with
```shell
$ git checkout -b new_branch_name
```
and make your changes on that branch.

**Do not commit directly to `master`.**

Commit messages should be short but decently descriptive of the changes made in that commit.

If you wish to merge with another branch, first checkout your branch and then
```shell
$ git merge target_branch
```
or if the changes are simple,
```shell
$ git rebase target_branch
```
Resolve any merge conflicts. Then, checkout the target branch and
```
$ git merge your_branch
```
Do not `rebase` on the target branch.