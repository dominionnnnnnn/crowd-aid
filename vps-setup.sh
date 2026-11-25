#!/bin/bash
# Paste this whole file into the "User Data" box when creating the instance.
# It runs ONCE as root on first boot. Progress log: /var/log/vps-setup.log
# Done when /root/SETUP_DONE exists (takes ~3-5 min after the instance is up).
set -eux
exec > /var/log/vps-setup.log 2>&1
export DEBIAN_FRONTEND=noninteractive

# The image's default login user (usually "ubuntu"); your keypair is installed for this user
DEFAULT_USER=$(getent passwd 1000 | cut -d: -f1)

# 1) Swap if RAM < 3 GB (the .NET build needs memory)
RAM_MB=$(free -m | awk '/Mem:/{print $2}')
if [ ! -f /swapfile ] && [ "$RAM_MB" -lt 3000 ]; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

# 2) Base packages + Docker
apt-get update
apt-get install -y ca-certificates curl git ufw fail2ban unattended-upgrades
curl -fsSL https://get.docker.com | sh
[ -n "$DEFAULT_USER" ] && usermod -aG docker "$DEFAULT_USER"

# 3) SSH: key-only, no root login (00- prefix wins over the cloud image's own settings)
cat > /etc/ssh/sshd_config.d/00-hardening.conf <<'EOF'
PermitRootLogin no
PasswordAuthentication no
KbdInteractiveAuthentication no
EOF
systemctl restart ssh || systemctl restart sshd

# 4) Firewall inside the server (security group is the outer firewall)
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443
ufw --force enable

# 5) Ban IPs that brute-force SSH
systemctl enable --now fail2ban

touch /root/SETUP_DONE
echo "SETUP COMPLETE"
