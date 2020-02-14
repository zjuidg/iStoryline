# iStoryline

This is a web server that implements the algorithms of [iStoryline](https://istoryline.github.io/).

## Dependencies

1. [Mosek 6](https://www.mosek.com/downloads/6/)
2. [ASP.NET Core 2.0](https://dotnet.microsoft.com/download/dotnet-core/2.0)

Please confirm the runtime environment: 64 bit or 32 bit. We recommend using the 64 bit versions of the softwares to keep compatible with Windows 10.

## Usage

```C#
dotnet run Start.cs
```

We host **iStoryline** interface on [http://localhost:5050](http://localhost:5050).

We also provide `post` method to obtain the storyline layout generated using [StoryFlow](http://www.ycwu.org/projects/infovis13.html) algorithms.

```python
# Python
ret = requests.post('http://localhost:5050/api/update', config)
print(ret)
```

### Post

```javascript
// Form Data
config = {
    "id": "path/to/story_script",
    "sessionInnerGap": 18, // inner gap of the sessions
    "sessionOuterGap": 54, // outer gap between sessions
    "sessionInnerGaps": [],
    "sessionOuterGaps": [],
    "majorCharacters": [],
    "orders": [],
    "groupIds": [],
    "selectedSessions": [],
    "orderTable": [],
    "sessionBreaks": []
}

// sessionInnerGaps
sessionInnerGaps = [{
  "item1": 0,  // session id
  "item2": 36  // inner gap of the session
},...]

// sessionOuterGaps
sessionOuterGaps = [{
  "item1": {
    "item1": 0, // session1 id
    "item2": 1  // session2 id
  }, // sessions pair
  "item2": {
    "item1": 50,  // lower bound of the gap
    "item2": 100, // upper bound of the gap
  } // outer gap between session1 and session2
},...]

// majorCharacters
majorCharacters = [{
  "item1": 0,      // character id
  "item2": [0, 2]  // time spans
},...]

// orders: character0 must be ahead of character1
orders = [[0, 1],...]

// sessionBreaks
sessionBreaks = [{
  "frame": 0,    // time span
  "session1": 0, // session1 id
  "session2": 1  // session2 id
},...]
```

## Data

Put [story scripts](https://github.com/tangtan/istoryline/wiki/Story-Script) under the directory of `./deploy/uploadFiles` (or you can change the path var in the appsetting.json).

## Reference

We appreciate your citation if this library contributes to your work.

```bibtex
@article{iStoryline2018,
  title = {{iStoryline: Effective Convergence to Hand-drawn Storylines}},
  author = {Tang, Tan and Rubab, Sadia and Lai, Jiewen and Cui, Weiwei and Yu, Lingyun and Wu, Yingcai},
  journal = {IEEE Transactions on Visualization and Computer Graphics},
  volume = {25},
  number = {1},
  pages = {769--778},
  year = {2018},
  publisher = {IEEE}
}
```
