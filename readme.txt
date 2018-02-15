The subfolders in the Configs folder are (recursively) copied into output, as a post build step when compiling the Platform project. 
  - If VS has no reason to build the Platform project (e.g., if none of its source files have changed), the changes made in the solution sources will not be copied over. 
  - To force copying over, use "rebuild all" command (which will make VS compile Platform)

For correct operation, ensure that the folder structure on disk matches what you want.

By default the folder structure that you see in VS Solution Explorer has nothing to do with how files sit on disk.

