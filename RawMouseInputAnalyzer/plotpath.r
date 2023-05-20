absolute_max = function(data) max(max(data), abs(min(data)))

centered_difference = function(data)
{
  return( centered_difference_full( data, (1:length(data)) ))
}

centered_difference_full <- function(data, axis) {
  
  n <- length(data)
  
  # Initialize a vector of length n to enter the derivative approximations
  fdx <- vector(length = n)
  
  # Iterate through the values using the forward differencing method
  for (i in 2:(n-1)) {
    fdx[i] <- (data[i+1] - data[i-1]) / (axis[i+1] - axis[i-1])
  }
  
  # For the last value, since we are unable to perform the forward differencing method 
  # as only the first n values are known, we use the backward differencing approach
  # instead. Note this will essentially give the same value as the last iteration 
  # in the forward differencing method, but it is used as an approximation as we 
  # don't have any more information
  fdx[n] <- (data[n] - data[n - 1]) / (axis[n] - axis[n - 1])
  fdx[1] <- (data[2] - data[1]) / (axis[2] - axis[1])
  
  return(fdx)
}

offset = function(data) { return(data - min(data)) }
normalize = function(data, min = -1, max = 1) { return( (scale(offset(data)) * (max - min)) + min) }
scale = function(data)
{
  abs_max = absolute_max(data)
  return(data / abs_max)
}

load_mousetrack = function(filename, filterfn = function(x) x)
{
  mousetrack = filterfn(read.csv(filename, sep = ";"))

  mousetrack$rX = normalize(mousetrack$X)
  mousetrack$rY = normalize(mousetrack$Y)
  mousetrack$rT = normalize(mousetrack$DeltaT)

  mousetrack$dX = scale(centered_difference(mousetrack$rX))
  mousetrack$dXdT = scale(centered_difference_full(mousetrack$rX, mousetrack$DeltaT))

  mousetrack$dY = scale(centered_difference(mousetrack$rY))
  mousetrack$dT = scale(centered_difference(mousetrack$rT))

  mousetrack$aX = scale(centered_difference(mousetrack$dX))
  mousetrack$aY = scale(centered_difference(mousetrack$dY))
  mousetrack$aT = scale(centered_difference(mousetrack$dT))

  return(mousetrack)
}

plot_track_comp = function(track)
{
  xdata = 0:(dim(track)[1]-1)
  plot(xdata, rep(0, each=length(xdata)), type="l", col="grey", lty=1, lwd=1, ylim=c(-1,1))
  #lines(xdata, track$aX, col="cyan", lty=1, lwd=1)
  #lines(xdata, track$rX, col="blue", lty=1, lwd=2)
  #lines(xdata, track$dX, col="blue", lty=1, lwd=1)
  #lines(xdata, track$aY, col="orange", lty=1, lwd=1)
  #lines(xdata, track$rY, col="red", lty=1, lwd=2)
  #lines(xdata, track$dY, col="red", lty=1, lwd=1)
  #lines(xdata, track$rT, col="green", lty=1, lwd=2)
  #lines(xdata, track$dT, col="green", lty=1, lwd=1)

  #lines(track$rT, track$dX, col="blue", lty=1, lwd=2)

  legend(1,1,legend=c("X","Y","T"), col=c("blue","red","green"), lty=c(1,1,1), ncol=1)
}

init_plot_by_x = function(xdata) plot(xdata, rep(0, each=length(xdata)), type="l", col="grey", lty=1, lwd=1, ylim=c(-1,0))

plot_track_by_t = function(track)
{
  xdata = track$DeltaT
  init_plot_by_x(xdata)
  lines(xdata, track$rX, col="blue", lty=1, lwd=2)
  points(xdata, track$dXdT, col="blue", type="p", pch=1)
#  lines(xdata, track$dXdT, col="blue", lty=1, lwd=1)
}

compare_plot = function(data_list, plotfn)
{
  scale_fact = 0
  for(x in (1:length(data_list)))
  {
    currentdata = data_list[[x]]
    scale_fact = max( scale_fact, absolute_max(currentdata$Y) )
  }
  for(x in (1:length(data_list)))
  {
    currentdata = data_list[[x]]
    plotfn(x, currentdata$X, currentdata$Y / scale_fact)
  }
}

library(rgl)

dev1_track = load_mousetrack("S:\\mousetrack-1903997.csv", function(x) x)
dev2_track = load_mousetrack("S:\\mousetrack-30672057.csv")

#plot3d(x=dev1_track$X, y=dev1_track$Y, z=dev1_track$DeltaT, xlab="X", ylab="Y", zlab="DeltaT")
#plot3d(x=dev2_track$X, y=dev2_track$Y, z=dev2_track$DeltaT, col="blue", add=TRUE)

#plot_track_comp(dev1_track)
#plot_track_comp(dev2_track)

#plot(dev1_track$DeltaT, dev1_track$Y*-1, type="l", lty=1, lwd=2)
#plot(dev2_track$DeltaT, dev2_track$Y*-1)
#plot_track_by_t(dev2_track)

init_plot_by_x(c(0, max(dev1_track$DeltaT, dev2_track$DeltaT)))

compare_plot(
    list(
        data.frame(X=dev1_track$DeltaT, Y=dev1_track$X),
        data.frame(X=dev2_track$DeltaT, Y=dev2_track$X)
    ),
    function(i, x, y)
    {
        if(i == 1) lines(x, y, col="blue", lty=1, lwd=2)
        if(i == 2) lines(x, y, col="red", lty=1, lwd=2)
    }
)

compare_plot(
    list(
        data.frame(X=dev1_track$DeltaT, Y=centered_difference_full(dev1_track$X, dev1_track$DeltaT)),
        data.frame(X=dev2_track$DeltaT, Y=centered_difference_full(dev2_track$X, dev2_track$DeltaT))
    ),
    function(i, x, y)
    {
        if(i == 1) lines(x, y, col="blue", lty=1, lwd=1)
        if(i == 2) lines(x, y, col="red", lty=1, lwd=1)
    }
)

