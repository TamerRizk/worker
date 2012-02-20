hasWorker = function() {
  //return false;
  return (typeof(Worker) != "undefined") ? true : false;
};

(function(){
  var i = 1000000;
  var x = 0;
  var y = 0;
  while(--i>0){
     x = Math.floor(Math.ceil(Math.random()*1000000)/(Math.ceil(Math.random()*1000)+1));
     if(x%19==0)
        y+=x;
  }
  hasWorker ? postMessage(y) : workermessage(y);
  arguments.callee();
})();