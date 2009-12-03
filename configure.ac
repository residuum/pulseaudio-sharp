dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])

AC_INIT([[PulseAudio#]],
	[0.0], 
	[https://bugs.edge.launchpad.net/pulseaudio-sharp/+filebug],
	[pulseaudio-sharp],
	[https://launchpad.net/pulseaudio-sharp])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_SRCDIR([src/Context.cs])

AM_INIT_AUTOMAKE([foreign tar-pax dist-lzma 1.11])
AM_SILENT_RULES([yes])

AC_DEFUN([FOO_PROG_CSC],
[
	AC_PATH_PROGS([CSC], [csc mono-csc gmcs], [no])
	AS_IF([test "x$CSC" = "xno"],
	      [AC_MSG_ERROR([[could not find C# compiler]])])
])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

FOO_PROG_CSC

PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
AC_SUBST([GLIB_SHARP_20_LIBS])

AC_CONFIG_FILES([
	Makefile
])

AC_OUTPUT

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:     ${prefix}
    C# Compiler:        ${CSC} ${CSCFLAGS}
    Mono Runtime:       ${MONO}

  Build/Development
    Release Build:      ${enable_release}
    NUnit:              ${have_nunit}

EOF