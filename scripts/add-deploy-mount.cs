//@auth 
//@req(pathFrom, nodeGroup)

var mountFrom = "${nodes.build.first.id}";
var envName = "${settings.targetEnv}".split(".")[0];
var mountTo = nodeGroup;

//TODO:move autoDeployFolder deterination logic to jem
var autoDeployFolder, type;
var resp = jelastic.env.control.GetEnvInfo(envName, session);
if (resp.result != 0) return resp;

var nodes = resp.nodes;
var autoDeploy = true; 
       
for (var i = 0, l = nodes.length; i < l; i++) {
       if (nodes[i].nodeGroup == nodeGroup) {
              type = nodes[i].nodeType;
              if (type == "glassfish3") {
                     autoDeploy = false;
                     //autoDeployFolder="/opt/"+type+"/glassfish/domains/domain1/autodeploy";
              } else if (type == "springboot") {
                     autoDeploy = false;
              } else {
                     var cmd = [
                         "f=/etc/jelastic/metainf.conf; [[ -f $f ]] && source $f",
                         "f=/var/lib/jelastic/libs/envinfo.lib; [[ -f $f ]] && source $f",
                         "f=/etc/jelastic/environment; [[ -f $f ]] && source $f",
                         "echo ${WEBROOT:-$Webroot_Path}"
                     ];
                     resp = jelastic.env.control.ExecCmdById(envName, session, nodes[i].id, toJSON([{
                            "command": cmd.join("\n")
                     }]) + "", true, "root");
                     if (resp.result != 0) return resp;

                     var webroot = resp.responses[0].out;
                     if (type.indexOf("glassfish") > -1) {
                        webroot = webroot.replace("webapps", "glassfish/domains/domain1/autodeploy")    
                     }
                     autoDeployFolder = webroot.replace("//", "/");                     
              }              
              break;
       }
}

if (!autoDeploy) {
   return {result: 0, response: "autodeploy/hot-redeploy is not supported for [" + type + "]"}
}

if (!autoDeployFolder) {
   return {result: 99, type: "error", message: "autodeploy folder is not defined for [" + type + "]"}
}

var resp = jelastic.env.file.RemoveMountPointByGroup(envName, session, mountTo, autoDeployFolder);
if (resp.result != 0) return resp;

resp = jelastic.env.file.AddMountPointByGroup(envName, session, mountTo, autoDeployFolder, 'nfs', null, pathFrom, mountFrom, 'auto-deploy-folder', false); 
return resp;
